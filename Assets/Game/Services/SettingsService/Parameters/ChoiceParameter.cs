using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public abstract class ChoiceParameter : IChoiceSettingsParameter
{
    public abstract string CategoryKey { get; }
    public abstract string SaveKey { get; }
    public string Value { get; private set; }
    public IReadOnlyList<SettingsChoice> Options { get; private set; } = Array.Empty<SettingsChoice>();
    public event Action<string> Changed;
    public event Action OptionsChanged;
    protected abstract string DefaultValue { get; }
    protected abstract IEnumerable<SettingsChoice> CreateOptions();
    protected abstract void ApplyValue(string id);

    public void RefreshOptions()
    {
        var list = new List<SettingsChoice>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in CreateOptions())
        {
            if (string.IsNullOrEmpty(option.Id) || !ids.Add(option.Id))
                throw new InvalidOperationException($"Invalid or duplicate option ID for '{SaveKey}': {option.Id}");
            list.Add(option);
        }
        Options = list.AsReadOnly();
        OptionsChanged?.Invoke();
    }

    protected bool Contains(string id)
    {
        foreach (var option in Options)
            if (option.Id == id) return true;
        return false;
    }

    public virtual UniTask Reset()
    {
        RefreshOptions();
        SetValue(DefaultValue);
        return UniTask.CompletedTask;
    }
    public virtual UniTask Load(string data)
    {
        RefreshOptions();
        SetValue(Contains(data) ? data : DefaultValue);
        return UniTask.CompletedTask;
    }
    public UniTask<(bool shouldSave, string data)> TrySave() => UniTask.FromResult((Value != null, Value));
    public void SetValue(string id)
    {
        if (!Contains(id)) throw new ArgumentException($"Unknown option for '{SaveKey}': {id}", nameof(id));
        ApplyValue(id);
        if (Value == id) return;
        Value = id;
        Changed?.Invoke(id);
    }
}
