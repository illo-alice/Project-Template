using System;
using VContainer.Unity;

public class PauseMenuInviteHandler : IStartable, IDisposable
{
    private readonly PauseMenuScreen _pauseMenuScreen;
    private readonly SteamService _steamService;
    
    public PauseMenuInviteHandler(PauseMenuScreen pauseMenuScreen, SteamService steamService)
    {
        _pauseMenuScreen = pauseMenuScreen;
        _steamService = steamService;
    }
    
    public void Start()
    {
        _pauseMenuScreen.Invite += Invite;
        _steamService.LobbyChanged += Refresh;
        Refresh();
    }

    public void Dispose()
    {
        _pauseMenuScreen.Invite -= Invite;
        _steamService.LobbyChanged -= Refresh;
    }

    private void Refresh() => _pauseMenuScreen.SetInviteAvailable(_steamService.CanInvite);

    private void Invite()
    {
        _steamService.OpenInviteOverlay();
    }
}
