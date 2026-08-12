using System;
using Avalonia;
using Avalonia.Controls;

namespace Informer.App.Localization;

public static class LocalizationManager
{
    public static event Action<AppLanguage>? LanguageChanged;

    public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Russian;

    private static ResourceDictionary? _active;

    public static void Apply(AppLanguage language)
    {
        var app = Application.Current;
        if (app is null) return;

        if (_active is not null)
        {
            app.Resources.MergedDictionaries.Remove(_active);
        }

        _active = language == AppLanguage.English ? BuildEnglish() : BuildRussian();
        app.Resources.MergedDictionaries.Add(_active);
        CurrentLanguage = language;

        LanguageChanged?.Invoke(language);
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

    private static ResourceDictionary BuildRussian()
    {
        var dict = new ResourceDictionary();

        dict["TrayHistory"] = "История уведомлений";
        dict["TraySettings"] = "Настройки";
        dict["TrayExit"] = "Выход";

        dict["HistoryTitle"] = "История уведомлений";
        dict["HistorySenderLabel"] = "Отправитель:";
        dict["HistoryRefreshButton"] = "Обновить";
        dict["ColHistoryTime"] = "Время";
        dict["ColHistorySender"] = "Отправитель";
        dict["ColHistoryType"] = "Тип";
        dict["ColHistoryDescription"] = "Описание";
        dict["ColHistoryRead"] = "Прочитано";
        dict["CtxCopyText"] = "Копировать текст";
        dict["CtxDelete"] = "Удалить";
        dict["AllSendersOption"] = "Все отправители";

        dict["SeverityError"] = "Ошибка";
        dict["SeverityWarning"] = "Предупреждение";
        dict["SeverityInfo"] = "Сообщение";

        dict["SettingsTitle"] = "Настройки";
        dict["SectionRetention"] = "Хранение истории";
        dict["RetentionLabel"] = "Удалять сообщения старше (дней):";
        dict["SectionSecurity"] = "Безопасность";
        dict["RequireApiKeyCheckbox"] = "Требовать API-ключ для приёма уведомлений";
        dict["RateLimitDescription"] = "Ограничение частоты запросов (анти-спам):";
        dict["RateLimitMaxLabel"] = "Максимум запросов:";
        dict["RateLimitWindowLabel"] = "за (сек):";
        dict["SectionNotifications"] = "Уведомления";
        dict["ToastDurationLabel"] = "Время показа тоста (сек):";
        dict["SectionDisplayPolicy"] = "Политика отображения";
        dict["DisplayPolicyDescription"] = "Какие типы сообщений показывать во всплывающих тостах (в истории видны всегда):";
        dict["ShowInfoCheckbox"] = "Обычные сообщения";
        dict["ShowWarningCheckbox"] = "Предупреждения";
        dict["ShowErrorCheckbox"] = "Ошибки";
        dict["SectionLanguage"] = "Язык интерфейса";
        dict["LanguageLabel"] = "Язык:";
        dict["SectionApiKeys"] = "API-ключи";
        dict["ApiKeyActive"] = "активен";
        dict["ApiKeyRevoked"] = "отозван";
        dict["RevokeButton"] = "Отозвать";
        dict["NewKeyWatermark"] = "Название ключа (например, 1С Основная база)";
        dict["AddKeyButton"] = "Добавить ключ";
        dict["CtxCopyKey"] = "Копировать ключ";
        dict["CtxDeleteKey"] = "Удалить";
        dict["SaveButton"] = "Сохранить";

        dict["ErrRetentionRequired"] = "Обязательно введите количество дней хранения истории.";
        dict["ErrRetentionMin"] = "Количество дней хранения должно быть не меньше 1.";
        dict["ErrToastSecondsRequired"] = "Обязательно введите время показа тоста.";
        dict["ErrRateMaxRequired"] = "Обязательно введите максимум запросов для анти-спама.";
        dict["ErrRateWindowRequired"] = "Обязательно введите окно времени (сек) для анти-спама.";
        dict["ErrSettingsEmpty"] = "Ошибка: таблица настроек пуста.";
        dict["MsgSaved"] = "Настройки сохранены.";
        dict["NewMessagesTooltip"] = "Новые сообщения";

        return dict;
    }

    private static ResourceDictionary BuildEnglish()
    {
        var dict = new ResourceDictionary();

        dict["TrayHistory"] = "Notification History";
        dict["TraySettings"] = "Settings";
        dict["TrayExit"] = "Exit";

        dict["HistoryTitle"] = "Notification History";
        dict["HistorySenderLabel"] = "Sender:";
        dict["HistoryRefreshButton"] = "Refresh";
        dict["ColHistoryTime"] = "Time";
        dict["ColHistorySender"] = "Sender";
        dict["ColHistoryType"] = "Type";
        dict["ColHistoryDescription"] = "Description";
        dict["ColHistoryRead"] = "Read";
        dict["CtxCopyText"] = "Copy text";
        dict["CtxDelete"] = "Delete";
        dict["AllSendersOption"] = "All senders";

        dict["SeverityError"] = "Error";
        dict["SeverityWarning"] = "Warning";
        dict["SeverityInfo"] = "Message";

        dict["SettingsTitle"] = "Settings";
        dict["SectionRetention"] = "History retention";
        dict["RetentionLabel"] = "Delete messages older than (days):";
        dict["SectionSecurity"] = "Security";
        dict["RequireApiKeyCheckbox"] = "Require API key to accept notifications";
        dict["RateLimitDescription"] = "Request rate limit (anti-spam):";
        dict["RateLimitMaxLabel"] = "Max requests:";
        dict["RateLimitWindowLabel"] = "per (sec):";
        dict["SectionNotifications"] = "Notifications";
        dict["ToastDurationLabel"] = "Toast display duration (sec):";
        dict["SectionDisplayPolicy"] = "Display policy";
        dict["DisplayPolicyDescription"] = "Which message types pop up as toasts (always visible in history):";
        dict["ShowInfoCheckbox"] = "Regular messages";
        dict["ShowWarningCheckbox"] = "Warnings";
        dict["ShowErrorCheckbox"] = "Errors";
        dict["SectionLanguage"] = "Interface language";
        dict["LanguageLabel"] = "Language:";
        dict["SectionApiKeys"] = "API keys";
        dict["ApiKeyActive"] = "active";
        dict["ApiKeyRevoked"] = "revoked";
        dict["RevokeButton"] = "Revoke";
        dict["NewKeyWatermark"] = "Key name (e.g. 1C Main Base)";
        dict["AddKeyButton"] = "Add key";
        dict["CtxCopyKey"] = "Copy key";
        dict["CtxDeleteKey"] = "Delete";
        dict["SaveButton"] = "Save";

        dict["ErrRetentionRequired"] = "Please enter the history retention period in days.";
        dict["ErrRetentionMin"] = "Retention period must be at least 1 day.";
        dict["ErrToastSecondsRequired"] = "Please enter the toast display duration.";
        dict["ErrRateMaxRequired"] = "Please enter the max requests for anti-spam.";
        dict["ErrRateWindowRequired"] = "Please enter the anti-spam time window (sec).";
        dict["ErrSettingsEmpty"] = "Error: settings table is empty.";
        dict["MsgSaved"] = "Settings saved.";
        dict["NewMessagesTooltip"] = "New messages";

        return dict;
    }
}