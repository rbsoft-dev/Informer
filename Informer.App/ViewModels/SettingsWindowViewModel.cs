using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Informer.App.Localization;
using Informer.Core.Entities;
using Informer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Informer.App.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject
{
    private static readonly HttpClient DownloadHttp = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly IServiceScopeFactory _scopeFactory;
    private bool _isLoadingLanguage;

    [ObservableProperty]
    private int? _retentionDays = 30;

    [ObservableProperty]
    private bool _requireApiKey = true;

    [ObservableProperty]
    private int? _toastDisplaySeconds = 8;

    [ObservableProperty]
    private bool _showInfoToasts = true;

    [ObservableProperty]
    private bool _showWarningToasts = true;

    [ObservableProperty]
    private bool _showErrorToasts = true;

    [ObservableProperty]
    private int? _rateLimitMaxRequests = 20;

    [ObservableProperty]
    private int? _rateLimitWindowSeconds = 10;

    [ObservableProperty]
    private int? _listenPort = 4399;

    public ObservableCollection<LanguageInfo> LanguageOptions { get; } = new();

    [ObservableProperty]
    private LanguageInfo? _selectedLanguage;

    [ObservableProperty]
    private string _languagePackUrl = string.Empty;

    [ObservableProperty]
    private string? _languagePackStatus;

    [ObservableProperty]
    private bool _isLanguagePackError;

    [ObservableProperty]
    private ObservableCollection<ApiKeyEntity> _apiKeys = new();

    [ObservableProperty]
    private string _newKeyLabel = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    public SettingsWindowViewModel()
    {
        _scopeFactory = Informer.App.Program.Services.GetRequiredService<IServiceScopeFactory>();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync();
        if (settings is not null)
        {
            RetentionDays = settings.RetentionDays;
            RequireApiKey = settings.RequireApiKey;
            ToastDisplaySeconds = settings.ToastDisplaySeconds;
            ShowInfoToasts = settings.ShowInfoToasts;
            ShowWarningToasts = settings.ShowWarningToasts;
            ShowErrorToasts = settings.ShowErrorToasts;
            RateLimitMaxRequests = settings.RateLimitMaxRequests;
            RateLimitWindowSeconds = settings.RateLimitWindowSeconds;
            ListenPort = settings.ListenPort;

            RefreshLanguageOptions();

            _isLoadingLanguage = true;
            SelectedLanguage = LanguageOptions.FirstOrDefault(l => l.Code == settings.Language)
                ?? LanguageOptions.FirstOrDefault();
            _isLoadingLanguage = false;
        }

        var keys = await db.ApiKeys.AsNoTracking()
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToListAsync();
        ApiKeys = new ObservableCollection<ApiKeyEntity>(keys);
    }
    private void RefreshLanguageOptions()
    {
        var current = SelectedLanguage;

        LanguageOptions.Clear();
        foreach (var lang in LocalizationManager.AvailableLanguages)
        {
            LanguageOptions.Add(lang);
        }

        if (current is not null)
        {
            var stillThere = LanguageOptions.FirstOrDefault(l => l.Code == current.Code);
            if (stillThere is not null)
            {
                _isLoadingLanguage = true;
                SelectedLanguage = stillThere;
                _isLoadingLanguage = false;
            }
        }
    }
    partial void OnSelectedLanguageChanged(LanguageInfo? value)
    {
        if (_isLoadingLanguage || value is null)
        {
            return;
        }

        LocalizationManager.Apply(value.Code);
        _ = PersistLanguageAsync(value.Code);
    }

    private async Task PersistLanguageAsync(string code)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var settings = await db.AppSettings.FirstOrDefaultAsync();
        if (settings is not null)
        {
            settings.Language = code;
            await db.SaveChangesAsync();
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (RetentionDays is null)
        {
            StatusMessage = LocalizationManager.Get("ErrRetentionRequired");
            IsStatusError = true;
            return;
        }

        if (RetentionDays < 1)
        {
            StatusMessage = LocalizationManager.Get("ErrRetentionMin");
            IsStatusError = true;
            return;
        }

        if (ToastDisplaySeconds is null)
        {
            StatusMessage = LocalizationManager.Get("ErrToastSecondsRequired");
            IsStatusError = true;
            return;
        }

        if (RateLimitMaxRequests is null)
        {
            StatusMessage = LocalizationManager.Get("ErrRateMaxRequired");
            IsStatusError = true;
            return;
        }

        if (RateLimitWindowSeconds is null)
        {
            StatusMessage = LocalizationManager.Get("ErrRateWindowRequired");
            IsStatusError = true;
            return;
        }

        if (ListenPort is null)
        {
            StatusMessage = LocalizationManager.Get("ErrPortRequired");
            IsStatusError = true;
            return;
        }

        if (ListenPort is < 1 or > 65535)
        {
            StatusMessage = LocalizationManager.Get("ErrPortRange");
            IsStatusError = true;
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var settings = await db.AppSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            StatusMessage = LocalizationManager.Get("ErrSettingsEmpty");
            IsStatusError = true;
            return;
        }
        var portChanged = settings.ListenPort != ListenPort.Value;

        settings.RetentionDays = RetentionDays.Value;
        settings.RequireApiKey = RequireApiKey;
        settings.ToastDisplaySeconds = ToastDisplaySeconds.Value;
        settings.ShowInfoToasts = ShowInfoToasts;
        settings.ShowWarningToasts = ShowWarningToasts;
        settings.ShowErrorToasts = ShowErrorToasts;
        settings.RateLimitMaxRequests = RateLimitMaxRequests.Value;
        settings.RateLimitWindowSeconds = RateLimitWindowSeconds.Value;
        settings.ListenPort = ListenPort.Value;

        await db.SaveChangesAsync();
        StatusMessage = portChanged
            ? LocalizationManager.Get("MsgSavedPortRestart")
            : LocalizationManager.Get("MsgSaved");
        IsStatusError = false;
    }

    [RelayCommand]
    private async Task DownloadLanguagePack()
    {
        if (string.IsNullOrWhiteSpace(LanguagePackUrl))
        {
            LanguagePackStatus = LocalizationManager.Get("ErrLangUrlRequired");
            IsLanguagePackError = true;
            return;
        }

        try
        {
            var bytes = await DownloadHttp.GetByteArrayAsync(LanguagePackUrl);

            var rawName = Path.GetFileName(new Uri(LanguagePackUrl).LocalPath);
            if (string.IsNullOrWhiteSpace(rawName)
                || !rawName.EndsWith(".po", StringComparison.OrdinalIgnoreCase)
                || rawName.Contains("..", StringComparison.Ordinal))
            {
                LanguagePackStatus = LocalizationManager.Get("ErrLangBadFile");
                IsLanguagePackError = true;
                return;
            }

            var langsDir = Path.Combine(AppContext.BaseDirectory, "Localization", "langs");
            Directory.CreateDirectory(langsDir);
            var destPath = Path.Combine(langsDir, rawName);

            await File.WriteAllBytesAsync(destPath, bytes);

            LocalizationManager.RescanAvailableLanguages();
            RefreshLanguageOptions();

            var newCode = Path.GetFileNameWithoutExtension(rawName);
            var newLang = LanguageOptions.FirstOrDefault(l => l.Code == newCode);
            if (newLang is not null)
            {
                SelectedLanguage = newLang;
            }

            LanguagePackStatus = LocalizationManager.Get("MsgLangInstalled");
            IsLanguagePackError = false;
            LanguagePackUrl = string.Empty;
        }
        catch (Exception ex)
        {
            LanguagePackStatus = $"{LocalizationManager.Get("ErrLangDownloadFailed")}: {ex.Message}";
            IsLanguagePackError = true;
        }
    }

    [RelayCommand]
    private async Task AddApiKey()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var entity = new ApiKeyEntity
        {
            Key = Guid.NewGuid().ToString("N"),
            Label = string.IsNullOrWhiteSpace(NewKeyLabel) ? "Без названия" : NewKeyLabel.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ApiKeys.Add(entity);
        await db.SaveChangesAsync();

        ApiKeys.Insert(0, entity);
        NewKeyLabel = string.Empty;
    }

    [RelayCommand]
    private async Task RevokeApiKey(ApiKeyEntity? key)
    {
        if (key is null) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var entity = await db.ApiKeys.FindAsync(key.Id);
        if (entity is null) return;

        entity.IsActive = false;
        await db.SaveChangesAsync();

        key.IsActive = false;
        var index = ApiKeys.IndexOf(key);
        if (index >= 0)
        {
            ApiKeys[index] = key;
        }
    }

    public async Task DeleteApiKeyAsync(ApiKeyEntity key)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var entity = await db.ApiKeys.FindAsync(key.Id);
        if (entity is not null)
        {
            db.ApiKeys.Remove(entity);
            await db.SaveChangesAsync();
        }

        ApiKeys.Remove(key);
    }
}