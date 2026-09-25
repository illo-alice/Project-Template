using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageParameter : IChoiceSettingsParameter
{
    private readonly Dictionary<string, Locale> _locales = new(StringComparer.OrdinalIgnoreCase);

    public string CategoryKey => ParameterCategory.GENERAL;
    public string SaveKey => $"{CategoryKey}.language";
    public string Value { get; private set; }
    public IReadOnlyList<SettingsChoice> Options { get; private set; } = Array.Empty<SettingsChoice>();
    public event Action<string> Changed;
    public event Action OptionsChanged;

    public async UniTask Reset()
    {
        await InitializeLocalization();
        SetValue(GetDefaultLocaleCode());
    }

    public async UniTask Load(string data)
    {
        await InitializeLocalization();
        var code = data?.Trim();

        // A removed locale must not invalidate the rest of the settings file.
        SetValue(!string.IsNullOrEmpty(code) && _locales.ContainsKey(code)
            ? code
            : GetDefaultLocaleCode());
    }

    public UniTask<(bool shouldSave, string data)> TrySave()
    {
        return UniTask.FromResult((!string.IsNullOrEmpty(Value), Value));
    }

    public void SetValue(string localeCode)
    {
        if (string.IsNullOrWhiteSpace(localeCode) || !_locales.TryGetValue(localeCode.Trim(), out var locale))
            throw new ArgumentException($"Locale '{localeCode}' is not available.", nameof(localeCode));

        LocalizationSettings.SelectedLocale = locale;
        var code = locale.Identifier.Code;
        if (string.Equals(Value, code, StringComparison.Ordinal))
            return;

        Value = code;
        Changed?.Invoke(Value);
    }

    private string GetDefaultLocaleCode()
    {
        // The default is configured in Localization Settings, not in the dropdown.
        var projectLocale = LocalizationSettings.ProjectLocale;
        if (projectLocale != null && _locales.ContainsKey(projectLocale.Identifier.Code))
            return projectLocale.Identifier.Code;

        var selectedLocale = LocalizationSettings.SelectedLocale;
        if (selectedLocale != null && _locales.ContainsKey(selectedLocale.Identifier.Code))
            return selectedLocale.Identifier.Code;

        throw new InvalidOperationException("No available default locale is configured in Localization Settings.");
    }

    private async UniTask InitializeLocalization()
    {
        var cancellationToken = Application.exitCancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        await LocalizationSettings.InitializationOperation.ToUniTask(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var locales = new Dictionary<string, Locale>(StringComparer.OrdinalIgnoreCase);
        var options = new List<SettingsChoice>();
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale == null)
                continue;

            var code = locale.Identifier.Code;
            if (string.IsNullOrWhiteSpace(code) || !locales.TryAdd(code, locale))
                throw new InvalidOperationException($"Empty or duplicate locale code: '{code}'.");

            var label = locale.Identifier.CultureInfo?.NativeName ?? locale.LocaleName;
            options.Add(new SettingsChoice(code, label));
        }

        if (options.Count == 0)
            throw new InvalidOperationException("No locales are configured in Localization Settings.");

        _locales.Clear();
        foreach (var pair in locales)
            _locales.Add(pair.Key, pair.Value);

        Options = options.AsReadOnly();
        OptionsChanged?.Invoke();
    }
}
