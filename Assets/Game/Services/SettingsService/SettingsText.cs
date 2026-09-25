using UnityEngine.Localization.Settings;

public static class SettingsText
{
    public static string Get(string key, string fallback)
    {
        if (!LocalizationSettings.InitializationOperation.IsDone) return fallback;
        var text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        return string.IsNullOrEmpty(text) || text.StartsWith("No translation found") ? fallback : text;
    }
}
