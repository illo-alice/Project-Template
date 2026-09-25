using System;
using Cysharp.Threading.Tasks;

public abstract class BoolParameter : IBoolSettingsParameter
{
    public abstract string CategoryKey { get; }
    public abstract string SaveKey { get; }
    protected virtual bool DefaultValue => false;
    public bool Value { get; private set; }
    public event Action<bool> Changed;

    public UniTask Reset() { SetValue(DefaultValue); return UniTask.CompletedTask; }
    public UniTask Load(string data)
    {
        if (!bool.TryParse(data, out var value))
            throw new FormatException($"Invalid boolean for '{SaveKey}': {data}");
        SetValue(value);
        return UniTask.CompletedTask;
    }
    public UniTask<(bool shouldSave, string data)> TrySave() => UniTask.FromResult((true, Value.ToString()));
    public void SetValue(bool value)
    {
        ApplyValue(value);
        if (Value == value) return;
        Value = value;
        Changed?.Invoke(value);
    }
    protected abstract void ApplyValue(bool value);
}
