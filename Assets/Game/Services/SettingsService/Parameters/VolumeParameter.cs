using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class VolumeParameter : IFloatSettingsParameter
{
    private const float DefaultValue = 1f;
    protected AudioService AudioService { get; }
    
    protected VolumeParameter(AudioService audioService)
    {
        AudioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
    }
    
    public string CategoryKey => ParameterCategory.AUDIO;
    public abstract string SaveKey { get; }
    
    public UniTask Reset()
    {
        SetValue(DefaultValue);
        return UniTask.CompletedTask;
    }

    public UniTask Load(string data)
    {
        if (!float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
            float.IsNaN(value) || float.IsInfinity(value))
            throw new FormatException($"Invalid saved value for '{SaveKey}'.");

        SetValue(value);
        return UniTask.CompletedTask;
    }

    public UniTask<(bool shouldSave, string data)> TrySave()
    {
        return UniTask.FromResult((true, Value.ToString("R", CultureInfo.InvariantCulture)));
    }

    public float Value { get; private set; } = DefaultValue;
    public event Action<float> Changed;
    public void SetValue(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(nameof(value), "Volume must be a finite number.");

        value = Mathf.Clamp01(value);
        
        ApplyVolume(value);
        if (Value == value)
            return;

        Value = value;
        Changed?.Invoke(Value);
    }
    protected abstract void ApplyVolume(float value);
}
