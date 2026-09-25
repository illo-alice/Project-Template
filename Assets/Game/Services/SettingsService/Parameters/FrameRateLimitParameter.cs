using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class FrameRateLimitParameter : IFloatSettingsParameter
{
    public const int Minimum = 30;
    public const int Maximum = 1000;
    private const int DefaultValue = 120;

    public string CategoryKey => ParameterCategory.GRAPHICS;
    public string SaveKey => $"{CategoryKey}.FrameRateLimit";
    public float Value { get; private set; } = DefaultValue;
    public event Action<float> Changed;

    public UniTask Reset()
    {
        SetValue(DefaultValue);
        return UniTask.CompletedTask;
    }

    public UniTask Load(string data)
    {
        SetValue(string.Equals(data, "unlimited", StringComparison.OrdinalIgnoreCase) ? Maximum
            : int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fps) ? fps : DefaultValue);
        return UniTask.CompletedTask;
    }

    public UniTask<(bool shouldSave, string data)> TrySave() =>
        UniTask.FromResult((true, Value == Maximum ? "unlimited" : Value.ToString("0", CultureInfo.InvariantCulture)));

    public void SetValue(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(nameof(value));

        value = Mathf.Round(Mathf.Clamp(value, Minimum, Maximum));
        Application.targetFrameRate = value == Maximum ? -1 : (int)value;
        if (Value == value)
            return;

        Value = value;
        Changed?.Invoke(value);
    }
}
