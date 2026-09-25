using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class PauseMenuSettingsHandler : IStartable, IDisposable
{
    private readonly PauseMenuScreen _pauseMenuScreen;
    private readonly SettingsScreen _settingsScreen;
    private readonly PauseMenuToggle _pauseMenuToggle;
    private readonly Session _session;
    private bool _opened;

    public PauseMenuSettingsHandler(PauseMenuScreen pauseMenuScreen, SettingsScreen settingsScreen,
        PauseMenuToggle pauseMenuToggle, Session session)
    {
        _pauseMenuScreen = pauseMenuScreen;
        _settingsScreen = settingsScreen;
        _pauseMenuToggle = pauseMenuToggle;
        _session = session;
    }

    public void Start()
    {
        _pauseMenuScreen.Settings += OnSettings;
        _settingsScreen.Hidden += OnHidden;
    }

    public void Dispose()
    {
        _pauseMenuScreen.Settings -= OnSettings;
        _settingsScreen.Hidden -= OnHidden;
    }

    private void OnSettings() => Show().Forget();

    private async UniTask Show()
    {
        if (_opened || !_session.IsStarted) return;
        _opened = true;
        _pauseMenuToggle.Block();
        try
        {
            await _pauseMenuScreen.Hide();
            if (!_session.IsStarted || !_pauseMenuToggle.IsShowing) { _opened = false; return; }
            await _settingsScreen.Show();
        }
        catch (OperationCanceledException) { _opened = false; }
    }

    private void OnHidden()
    {
        if (!_opened) return;
        _opened = false;
        if (!_session.IsStarted || !_pauseMenuToggle.IsShowing) return;
        _pauseMenuScreen.Show().Forget();
        _pauseMenuToggle.UnBlock();
    }
}
