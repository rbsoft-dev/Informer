using Avalonia;
using Avalonia.Controls;
using Karambolo.PO;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Informer.App.Localization;
public sealed record LanguageInfo(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}
public static class LocalizationManager
{
    public static event Action<string>? LanguageChanged;

    public static string CurrentLanguage { get; private set; } = "ru";

    public static IReadOnlyList<LanguageInfo> AvailableLanguages { get; private set; } = Array.Empty<LanguageInfo>();

    private static ResourceDictionary? _active;

    private static readonly POParser Parser = new(new POParserSettings
    {
        SkipInfoHeaders = false
    });

    private static string LangsDirectory => Path.Combine(AppContext.BaseDirectory, "Localization", "langs");
    public static void Apply(string languageCode)
    {
        var app = Application.Current;
        if (app is null) return;

        if (_active is not null)
        {
            app.Resources.MergedDictionaries.Remove(_active);
        }

        _active = LoadPoAsResourceDictionary(languageCode + ".po");
        app.Resources.MergedDictionaries.Add(_active);
        CurrentLanguage = languageCode;

        LanguageChanged?.Invoke(languageCode);
    }

    public static string Get(string key)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var value) == true
            && value is string s)
        {
            return s;
        }

        return key;
    }

    private static ResourceDictionary LoadPoAsResourceDictionary(string fileName)
    {
        var dict = new ResourceDictionary();
        var path = Path.Combine(LangsDirectory, fileName);

      
        var fileText = File.ReadAllText(path, System.Text.Encoding.UTF8);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileText));
        var result = Parser.Parse(stream);

        if (result.Success)
        {
            foreach (var entry in result.Catalog)
            {
                var key = entry.Key.Id;
                if (string.IsNullOrEmpty(key)) continue;

                var text = entry.Count > 0 ? entry[0] : null;
                if (!string.IsNullOrEmpty(text))
                {
                    dict[key] = text;
                }

            }

        }

        return dict;
    }
    private static string GetNativeDisplayName(string code)
    {
        try
        {
            var name = new System.Globalization.CultureInfo(code).NativeName;
            var parenIndex = name.IndexOf(" (", StringComparison.Ordinal);
            return parenIndex > 0 ? name[..parenIndex] : name;
        }
        catch
        {
            return code.ToUpperInvariant();
        }
    }
    public static void RescanAvailableLanguages()
    {
        var displayNames = LoadLangIni();
        var list = new List<LanguageInfo>();

        if (Directory.Exists(LangsDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(LangsDirectory, "*.po"))
            {
                var code = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(code)) continue;

                var displayName = displayNames.TryGetValue(code, out var fromIni) && !string.IsNullOrWhiteSpace(fromIni)
                    ? fromIni
                    : GetNativeDisplayName(code);

                list.Add(new LanguageInfo(code, displayName));
            }
        }

        if (list.Count == 0)
        {
            list.Add(new LanguageInfo("ru", "Русский"));
        }

        AvailableLanguages = list.OrderBy(l => l.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static Dictionary<string, string> LoadLangIni()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        var path = Path.Combine(LangsDirectory, "lang.ini");
        if (!File.Exists(path))
        {
            return result;
        }

        var config = new ConfigurationBuilder()
            .AddIniFile(path, optional: true)
            .Build();

        foreach (var section in config.AsEnumerable())
        {
            if (!string.IsNullOrEmpty(section.Key) && section.Value is not null)
            {
                result[section.Key] = section.Value;
            }
        }

        return result;
    }
}