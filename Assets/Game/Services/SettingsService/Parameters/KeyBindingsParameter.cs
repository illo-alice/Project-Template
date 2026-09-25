using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class KeyBindingsParameter : ISettingsParameter, IDisposable
{
    public readonly struct BindingEntry
    {
        public readonly string Id;
        public readonly string Label;
        public BindingEntry(string id, string label) { Id = id; Label = label; }
    }
    private readonly GameInputService _input;
    private InputActionRebindingExtensions.RebindingOperation _operation;
    private InputAction _rebindingAction;
    private bool _wasEnabled;
    private int _finishedFrame = -1;
    public string CategoryKey => ParameterCategory.CONTROLS;
    public string SaveKey => $"{CategoryKey}.KeyBindings";
    public bool IsRebinding => _operation != null;
    public bool BlocksMenuInput => IsRebinding || Time.frameCount == _finishedFrame;
    public string ActiveBindingId { get; private set; }
    public string Status { get; private set; }
    public event Action Changed;

    public KeyBindingsParameter(GameInputService input) => _input = input;

    public IReadOnlyList<BindingEntry> GetBindings()
    {
        var result = new List<BindingEntry>();
        foreach (var map in _input.Actions.actionMaps)
        {
            // UI navigation and Look axes must remain usable while rebinding buttons.
            if (map.name != "Player" && map.name != "Voice") continue;
            foreach (var action in map.actions)
            {
                if (action.name == "Look") continue;
                foreach (var binding in action.bindings)
                {
                    if (binding.isComposite || !IsKeyboardMouse(binding)) continue;
                    var label = SettingsText.Get("action_" + action.name.ToLowerInvariant(), action.name);
                    if (binding.isPartOfComposite)
                        label += " / " + SettingsText.Get("binding_" + binding.name.ToLowerInvariant(), binding.name);
                    result.Add(new BindingEntry(binding.id.ToString(), label));
                }
            }
        }
        return result;
    }

    private static bool IsKeyboardMouse(InputBinding binding) =>
        binding.path != null && (binding.path.StartsWith("<Keyboard>", StringComparison.Ordinal) || binding.path.StartsWith("<Mouse>", StringComparison.Ordinal));

    private (InputAction action, int index) Find(string id)
    {
        foreach (var action in _input.Actions)
            for (var i = 0; i < action.bindings.Count; i++)
                if (action.bindings[i].id.ToString() == id) return (action, i);
        throw new ArgumentException($"Unknown binding ID: {id}", nameof(id));
    }

    public string GetDisplayName(string id)
    {
        var (action, index) = Find(id);
        // Use the binding's physical control path and static layout names.
        // Resolved Keyboard controls expose characters from the current OS layout (W -> Ц).
        return action.bindings[index].ToDisplayString(
            InputBinding.DisplayStringOptions.DontIncludeInteractions, control: null);
    }

    public void BeginRebind(string id)
    {
        CancelRebind();
        var (action, index) = Find(id);
        var binding = action.bindings[index];
        if (binding.isComposite || !IsKeyboardMouse(binding)) throw new InvalidOperationException("Binding cannot be rebound by this control.");
        _wasEnabled = action.enabled;
        _rebindingAction = action;
        action.Disable();
        ActiveBindingId = id;
        Status = SettingsText.Get("rebind_wait", "Press a key (Esc to cancel)");
        try
        {
            _operation = action.PerformInteractiveRebinding(index)
                .WithExpectedControlType("Button")
                .WithControlsHavingToMatchPath("<Keyboard>/*")
                .WithControlsHavingToMatchPath("<Mouse>/*")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithTimeout(10f)
                .OnMatchWaitForAnother(0.1f)
                .OnApplyBinding((_, path) =>
                {
                    // Do not silently steal another action's control.
                    var conflict = GetBindings().Any(entry =>
                    {
                        if (entry.Id == id) return false;
                        var other = Find(entry.Id);
                        return string.Equals(other.action.bindings[other.index].effectivePath, path, StringComparison.OrdinalIgnoreCase);
                    });
                    if (conflict)
                        Status = SettingsText.Get("rebind_conflict", "This key is already assigned. Choose another key.");
                    else
                    {
                        action.ApplyBindingOverride(index, path);
                        Status = null;
                    }
                })
                .OnCancel(_ => Finish(false))
                .OnComplete(_ => Finish(true));
            _operation.Start();
            Changed?.Invoke();
        }
        catch
        {
            Finish(false);
            throw;
        }
    }

    private void Finish(bool completed)
    {
        _finishedFrame = Time.frameCount;
        var operation = _operation;
        _operation = null;
        if (_rebindingAction != null && _wasEnabled) _rebindingAction.Enable();
        _rebindingAction = null;
        ActiveBindingId = null;
        if (!completed) Status = null;
        operation?.Dispose();
        Changed?.Invoke();
    }
    public void CancelRebind() => _operation?.Cancel();
    public UniTask Reset()
    {
        CancelRebind();
        _input.Actions.RemoveAllBindingOverrides();
        Status = null;
        Changed?.Invoke();
        return UniTask.CompletedTask;
    }
    public void ResetBinding(string id)
    {
        CancelRebind();
        var (action, index) = Find(id);
        var defaultPath = action.bindings[index].path;
        if (GetBindings().Any(entry =>
            {
                if (entry.Id == id) return false;
                var other = Find(entry.Id);
                return string.Equals(other.action.bindings[other.index].effectivePath, defaultPath, StringComparison.OrdinalIgnoreCase);
            }))
        {
            Status = SettingsText.Get("rebind_conflict", "This key is already assigned. Choose another key.");
            Changed?.Invoke();
            return;
        }
        action.RemoveBindingOverride(index);
        Status = null;
        Changed?.Invoke();
    }
    public UniTask Load(string data)
    {
        CancelRebind();
        var previous = _input.Actions.SaveBindingOverridesAsJson();
        try { _input.Actions.LoadBindingOverridesFromJson(data, true); }
        catch { _input.Actions.LoadBindingOverridesFromJson(previous, true); throw; }
        Changed?.Invoke();
        return UniTask.CompletedTask;
    }
    public UniTask<(bool shouldSave, string data)> TrySave()
    {
        CancelRebind();
        return UniTask.FromResult((true, _input.Actions.SaveBindingOverridesAsJson()));
    }
    public void Dispose() => CancelRebind();
}
