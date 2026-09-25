using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class MainMenuHostHandler : IStartable, IDisposable
{
    private readonly MainMenuScreen _mainMenuScreen;
    private readonly HostGameScreen _hostScreen;
    private readonly ChooseSaveScreen _chooseSaveScreen;

    public MainMenuHostHandler(MainMenuScreen mainMenuScreen, HostGameScreen hostScreen, ChooseSaveScreen chooseSaveScreen)
    {
        _mainMenuScreen = mainMenuScreen;
        _hostScreen = hostScreen;
        _chooseSaveScreen = chooseSaveScreen;
    }

    public void Start() => _mainMenuScreen.Host += Show;

    private void Show()
    {
        _hostScreen.Show(instant: true).Forget();
        _chooseSaveScreen.Show().Forget();
    }

    public void Dispose() => _mainMenuScreen.Host -= Show;
}
