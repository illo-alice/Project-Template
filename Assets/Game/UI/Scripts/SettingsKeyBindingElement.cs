using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsKeyBindingElement : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _value;
    [SerializeField] private Button _rebind;
    [SerializeField] private Button _reset;
    private KeyBindingsParameter _parameter;
    private string _id;
    public void Bind(KeyBindingsParameter parameter, KeyBindingsParameter.BindingEntry entry)
    {
        if (_parameter != null) _parameter.Changed -= Refresh;
        _parameter = parameter;
        _id = entry.Id;
        _label.text = entry.Label;
        _parameter.Changed += Refresh;
        Refresh();
    }
    private void OnEnable() { _rebind.onClick.AddListener(Rebind); _reset.onClick.AddListener(ResetBinding); }
    private void OnDisable()
    {
        _rebind.onClick.RemoveListener(Rebind);
        _reset.onClick.RemoveListener(ResetBinding);
        if (_parameter?.ActiveBindingId == _id) _parameter.CancelRebind();
    }
    private void OnDestroy() { if (_parameter != null) _parameter.Changed -= Refresh; }
    private void Rebind() => _parameter?.BeginRebind(_id);
    private void ResetBinding() => _parameter?.ResetBinding(_id);
    private void Refresh()
    {
        _value.text = _parameter.ActiveBindingId == _id ? "…" : _parameter.GetDisplayName(_id);
        _reset.interactable = !_parameter.IsRebinding;
    }
}
