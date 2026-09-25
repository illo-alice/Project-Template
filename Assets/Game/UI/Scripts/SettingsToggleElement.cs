using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsToggleElement : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;
    private IBoolSettingsParameter _parameter;
    public void Bind(IBoolSettingsParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException(nameof(parameter));
        Unbind();
        _parameter = parameter;
        _parameter.Changed += Refresh;
        Refresh(parameter.Value);
    }
    private void Refresh(bool value) => _toggle.SetIsOnWithoutNotify(value);
    private void OnEnable() { _toggle.onValueChanged.AddListener(OnChanged); if (_parameter != null) Refresh(_parameter.Value); }
    private void OnDisable() => _toggle.onValueChanged.RemoveListener(OnChanged);
    private void OnChanged(bool value)
    {
        if (_parameter == null) return;
        try { _parameter.SetValue(value); }
        finally { Refresh(_parameter.Value); }
    }
    public void Unbind() { if (_parameter != null) _parameter.Changed -= Refresh; _parameter = null; }
    private void OnDestroy() => Unbind();
}
