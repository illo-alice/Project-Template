using System;
using System.Collections.Generic;

public readonly struct SettingsChoice
{
    public string Id { get; }
    public string Label { get; }

    public SettingsChoice(string id, string label)
    {
        Id = id;
        Label = label;
    }
}

public interface IChoiceSettingsParameter : ISettingsParameter
{
    string Value { get; }
    IReadOnlyList<SettingsChoice> Options { get; }
    event Action<string> Changed;
    event Action OptionsChanged;
    void SetValue(string id);
}
