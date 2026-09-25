using VContainer.Unity;

public class LobbyFlow : IStartable
{
    private PauseMenuToggle _pauseMenuToggle;
    
    public LobbyFlow(PauseMenuToggle pauseMenuToggle)
    {
        _pauseMenuToggle = pauseMenuToggle;
    }
    
    public void Start()
    {
        _pauseMenuToggle.UnBlock();
    }
}
