using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class MenuFlow : IStartable
{
    private readonly MainMenuScreen _mainMenuScreen;
    private readonly PauseMenuToggle _pause;

    public MenuFlow(MainMenuScreen mainMenuScreen, PauseMenuToggle pause)
    {
        _mainMenuScreen = mainMenuScreen;
        _pause = pause;
    }

    public void Start()
    {
        _pause.ResetAndBlock();
        _mainMenuScreen.Show(instant: true).Forget();
    }
}
