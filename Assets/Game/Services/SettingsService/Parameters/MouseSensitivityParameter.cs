using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class MouseSensitivityParameter : IFloatSettingsParameter
{
    public const float Minimum = 0.1f;
    public const float Maximum = 10f;
    public string CategoryKey => ParameterCategory.CONTROLS;
    public string SaveKey => $"{CategoryKey}.MouseSensitivity";
    public float Value { get; private set; } = 1f;
    public event Action<float> Changed;
    public UniTask Reset() { SetValue(1f); return UniTask.CompletedTask; }
    public UniTask Load(string data)
    {
        if (!float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new FormatException($"Invalid sensitivity: {data}");
        SetValue(value);
        return UniTask.CompletedTask;
    }
    public UniTask<(bool shouldSave, string data)> TrySave() => UniTask.FromResult((true, Value.ToString("R", CultureInfo.InvariantCulture)));
    public void SetValue(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
        value = Mathf.Clamp(value, Minimum, Maximum);
        ApplyValue(value);
        if (Value == value) return;
        Value = value;
        Changed?.Invoke(value);
    }
    private void ApplyValue(float value)
    {
        // Connect to the camera controller when it is implemented.
    }
}
