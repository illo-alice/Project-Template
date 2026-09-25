using UnityEngine.Localization.Settings;

public class S
{
    public static string UI(string key)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
    }
}
