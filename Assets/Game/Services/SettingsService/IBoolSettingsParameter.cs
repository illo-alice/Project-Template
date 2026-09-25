using System;

public interface IBoolSettingsParameter : ISettingsParameter
{
    bool Value { get; }
    event Action<bool> Changed;
    void SetValue(bool value);
}
