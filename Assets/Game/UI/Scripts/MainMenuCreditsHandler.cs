using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class MainMenuCreditsHandler : IStartable, IDisposable
{
    private readonly CreditsScreen _creditsScreen;
    private readonly MainMenuScreen _mainMenuScreen;
    
    public MainMenuCreditsHandler(MainMenuScreen mainMenuScreen, CreditsScreen creditsScreen)
    {
        _creditsScreen = creditsScreen;
        _mainMenuScreen = mainMenuScreen;
    }
    
    public void Start()
    {
        _mainMenuScreen.Credits += _creditsScreen.Show;
        _creditsScreen.Back += OnBack;
    }

    private void OnBack()
    {
        _creditsScreen.Hide();
        _mainMenuScreen.Show().Forget();
    }

    public void Dispose()
    {
        _mainMenuScreen.Credits -= _creditsScreen.Show;
        _creditsScreen.Back -= OnBack;
    }
}
