using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuScreen : MonoBehaviour
{
    [SerializeField] private CanvasFader _fader;
    [SerializeField] private Button _host;
    [SerializeField] private Button _join;
    [SerializeField] private Button _settings;
    [SerializeField] private Button _credits;
    [SerializeField] private Button _quit;

    public event Action Host;
    public event Action Join;
    public event Action Settings;
    public event Action Credits;
    public event Action Quit;

    private void Awake()
    {
        _host.onClick.AddListener(OnHost);
        _join?.onClick.AddListener(OnJoin);
        _settings.onClick.AddListener(OnSettings);
        _credits.onClick.AddListener(OnCredits);
        _quit.onClick.AddListener(OnQuit);
    }

    public UniTask Show(bool instant = false) => _fader.Show(instant);
    public UniTask Hide(bool instant = false) => _fader.Hide(instant);
    public void SetInteractable(bool interactable) => _fader.SetInteractable(interactable);

    private void OnHost() => Host?.Invoke();
    private void OnJoin() => Join?.Invoke();
    private void OnSettings() => Settings?.Invoke();
    private void OnCredits() => Credits?.Invoke();
    private void OnQuit() => Quit?.Invoke();

    private void OnDestroy()
    {
        if (_host != null) _host.onClick.RemoveListener(OnHost);
        if (_join != null) _join.onClick.RemoveListener(OnJoin);
        if (_settings != null) _settings.onClick.RemoveListener(OnSettings);
        if (_credits != null) _credits.onClick.RemoveListener(OnCredits);
        if (_quit != null) _quit.onClick.RemoveListener(OnQuit);
    }
}
