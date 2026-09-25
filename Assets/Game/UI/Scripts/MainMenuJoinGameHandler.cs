using VContainer.Unity;

public class MainMenuJoinGameHandler : IStartable
{
    private MainMenuScreen _mainMenuScreen;
    private JoinScreen _joinScreen;
    
    public MainMenuJoinGameHandler(MainMenuScreen mainMenuScreen, JoinScreen joinScreen)
    {
        _mainMenuScreen = mainMenuScreen;
        _joinScreen = joinScreen;
    }
    
    public void Start()
    {
        _mainMenuScreen.Join += _joinScreen.ShowVoid;
    }
}
