using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class MainMenuSettingsHandler : IStartable, IDisposable
{
    private readonly SettingsScreen _settingsScreen;
    private readonly MainMenuScreen _mainMenuScreen;
    private bool _openedFromMenu;

    public MainMenuSettingsHandler(MainMenuScreen mainMenuScreen, SettingsScreen settingsScreen)
    {
        _settingsScreen = settingsScreen;
        _mainMenuScreen = mainMenuScreen;
    }

    public void Start()
    {
        _mainMenuScreen.Settings += OnSettings;
        _settingsScreen.Hidden += OnSettingsHidden;
    }

    private void OnSettings()
    {
        _openedFromMenu = true;
        _settingsScreen.Show().Forget();
    }

    private void OnSettingsHidden()
    {
        if (!_openedFromMenu) return;
        _openedFromMenu = false;
        _mainMenuScreen.Show().Forget();
    }

    public void Dispose()
    {
        _mainMenuScreen.Settings -= OnSettings;
        _settingsScreen.Hidden -= OnSettingsHidden;
    }
}
