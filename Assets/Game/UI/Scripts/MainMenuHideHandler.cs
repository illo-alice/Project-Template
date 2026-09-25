using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class MainMenuHideHandler : IStartable, IDisposable
{
    private readonly MainMenuScreen _mainMenuScreen;

    public MainMenuHideHandler(MainMenuScreen mainMenuScreen)
    {
        _mainMenuScreen = mainMenuScreen;
    }

    public void Start()
    {
        _mainMenuScreen.Host += Hide;
        _mainMenuScreen.Join += Hide;
        _mainMenuScreen.Settings += Hide;
        _mainMenuScreen.Credits += Hide;
        _mainMenuScreen.Quit += Hide;
    }

    private void Hide()
    {
        _mainMenuScreen.SetInteractable(false);
        _mainMenuScreen.Hide().Forget();
    }

    public void Dispose()
    {
        _mainMenuScreen.Host -= Hide;
        _mainMenuScreen.Join -= Hide;
        _mainMenuScreen.Settings -= Hide;
        _mainMenuScreen.Credits -= Hide;
        _mainMenuScreen.Quit -= Hide;
    }
}
