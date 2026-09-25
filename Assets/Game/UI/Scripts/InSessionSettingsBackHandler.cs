using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class InSessionSettingsBackHandler : IStartable, IDisposable
{
    private PauseMenuScreen _pauseMenuScreen;
    private PauseMenuToggle _pauseMenuToggle;
    private SettingsScreen _settingsScreen;
    
    public InSessionSettingsBackHandler(SettingsScreen settingsScreen, PauseMenuScreen pauseMenuScreen, PauseMenuToggle pauseMenuToggle)
    {
        _settingsScreen = settingsScreen;
        _pauseMenuScreen = pauseMenuScreen;
        _pauseMenuToggle = pauseMenuToggle;
    }
    
    public void Start()
    {
        _settingsScreen.Back += Show;
    }

    public void Dispose()
    {
        _settingsScreen.Back -= Show;
    }

    private void Show()
    {
        SettingsScreenOnBack().Forget();
    }
    
    private async UniTask SettingsScreenOnBack()
    {
        await _pauseMenuScreen.Show();
        _pauseMenuToggle.UnBlock();
    }
}
