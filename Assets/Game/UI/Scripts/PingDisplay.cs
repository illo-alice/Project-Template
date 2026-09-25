using TMPro;
using UnityEngine;
using VContainer;

public sealed class PingDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    private Session _session;
    private ShowPingParameter _setting;
    private float _nextUpdate;
    [Inject] public void Construct(Session session, ShowPingParameter setting) { _session = session; _setting = setting; }
    private void Update()
    {
        if (_setting == null || _text == null) return;
        _text.enabled = _setting.Value;
        if (!_setting.Value || Time.unscaledTime < _nextUpdate) return;
        _nextUpdate = Time.unscaledTime + 0.5f;
        _text.text = _session.TryGetPing(out var milliseconds) ? $"{milliseconds} ms" : "— ms";
    }
}
