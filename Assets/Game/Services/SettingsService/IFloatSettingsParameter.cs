using System;

public interface IFloatSettingsParameter : ISettingsParameter
{
    float Value { get; }
    event Action<float> Changed;
    void SetValue(float value);
}

public interface IEnumSettingsParameter<T> : ISettingsParameter where T : Enum
{
    T Value { get; }
    event Action<T> Changed;
    void SetValue(T value);
}
