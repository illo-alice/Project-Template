using System;
using VContainer.Unity;

public class PauseMenuContinueHandler : IStartable, IDisposable
{
    private readonly PauseMenuScreen _screen;
    private readonly PauseMenuToggle _pauseMenuToggle;
    
    public PauseMenuContinueHandler(PauseMenuScreen screen, PauseMenuToggle pauseMenuToggle)
    {
        _screen = screen;
        _pauseMenuToggle = pauseMenuToggle;
    }
    
    public void Start()
    {
        _screen.Continue += _pauseMenuToggle.Toggle;
    }

    public void Dispose()
    {
        _screen.Continue -= _pauseMenuToggle.Toggle;
    }
}
