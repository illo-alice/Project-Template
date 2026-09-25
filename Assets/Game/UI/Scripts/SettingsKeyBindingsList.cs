using TMPro;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class SettingsKeyBindingsList : MonoBehaviour
{
    [SerializeField] private SettingsKeyBindingElement _rowPrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private TMP_Text _status;
    private KeyBindingsParameter _parameter;
    private IObjectResolver _resolver;
    public void Bind(KeyBindingsParameter parameter, IObjectResolver resolver)
    {
        if (_parameter != null) _parameter.Changed -= RefreshStatus;
        _parameter = parameter;
        _resolver = resolver;
        _parameter.Changed += RefreshStatus;
        Rebuild();
    }
    public void Rebuild()
    {
        if (_parameter == null) return;
        _parameter.CancelRebind();
        foreach (Transform child in _content) Destroy(child.gameObject);
        foreach (var entry in _parameter.GetBindings())
            _resolver.Instantiate(_rowPrefab, _content).Bind(_parameter, entry);
        RefreshStatus();
    }
    private void RefreshStatus() => _status.text = _parameter.Status ?? SettingsText.Get("rebind_hint", "Click a key to change it. Esc cancels.");
    private void OnDestroy() { if (_parameter != null) _parameter.Changed -= RefreshStatus; }
}
