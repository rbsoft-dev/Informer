using System;
using System.Collections.ObjectModel;
using System.Linq;
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

    public ObservableCollection<string> LanguageOptions { get; } = new() { "Русский", "English" };

    [ObservableProperty]
    private string _selectedLanguageDisplay = "Русский";

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

            _isLoadingLanguage = true;
            SelectedLanguageDisplay = settings.Language == "en" ? "English" : "Русский";
            _isLoadingLanguage = false;
        }

        var keys = await db.ApiKeys.AsNoTracking()
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToListAsync();
        ApiKeys = new ObservableCollection<ApiKeyEntity>(keys);
    }

    partial void OnSelectedLanguageDisplayChanged(string value)
    {
        if (_isLoadingLanguage)
        {
            return;
        }

        var language = value == "English" ? AppLanguage.English : AppLanguage.Russian;
        LocalizationManager.Apply(language);
        _ = PersistLanguageAsync(language);
    }

    private async Task PersistLanguageAsync(AppLanguage language)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var settings = await db.AppSettings.FirstOrDefaultAsync();
        if (settings is not null)
        {
            settings.Language = language == AppLanguage.English ? "en" : "ru";
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

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var settings = await db.AppSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            StatusMessage = LocalizationManager.Get("ErrSettingsEmpty");
            IsStatusError = true;
            return;
        }

        settings.RetentionDays = RetentionDays.Value;
        settings.RequireApiKey = RequireApiKey;
        settings.ToastDisplaySeconds = ToastDisplaySeconds.Value;
        settings.ShowInfoToasts = ShowInfoToasts;
        settings.ShowWarningToasts = ShowWarningToasts;
        settings.ShowErrorToasts = ShowErrorToasts;
        settings.RateLimitMaxRequests = RateLimitMaxRequests.Value;
        settings.RateLimitWindowSeconds = RateLimitWindowSeconds.Value;

        await db.SaveChangesAsync();
        StatusMessage = LocalizationManager.Get("MsgSaved");
        IsStatusError = false;
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