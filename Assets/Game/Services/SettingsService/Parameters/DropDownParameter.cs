using System;
using Cysharp.Threading.Tasks;

public abstract class DropDownParameter<T> : IEnumSettingsParameter<T> where T : Enum
{
    public abstract string CategoryKey { get; }
    public abstract string SaveKey { get; }
    
    protected virtual T DefaultValue => default;
    
    public virtual UniTask Reset()
    {
        SetValue(DefaultValue);
        return UniTask.CompletedTask;
    }

    public virtual UniTask Load(string data)
    {
        if (Enum.TryParse(typeof(T), data, true, out var parsed) && Enum.IsDefined(typeof(T), parsed))
        {
            SetValue((T)parsed);
            return UniTask.CompletedTask;
        }
        
        throw new FormatException($"Invalid saved value for '{SaveKey}': {data}");
    }

    public virtual UniTask<(bool shouldSave, string data)> TrySave()
    {
        return UniTask.FromResult((true, Value.ToString()));
    }

    public T Value { get; private set; }
    public event Action<T> Changed;
    public void SetValue(T value)
    {
        if (!Enum.IsDefined(typeof(T), value))
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown value for '{SaveKey}'.");

        var previous = Value;
        Value = value;
        try
        {
            ApplyValue();
        }
        catch
        {
            Value = previous;
            throw;
        }

        if (Equals(previous, value))
            return;

        Changed?.Invoke(value);
    }

    public abstract void ApplyValue();
}
