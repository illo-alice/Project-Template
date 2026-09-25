using VContainer.Unity;

public class ClientRoomCodeReceiver : IStartable
{
    private SteamService _steamService;
    private RoomCodeService _roomCodeService;

    public ClientRoomCodeReceiver(SteamService steamService, RoomCodeService roomCodeService)
    {
        _steamService = steamService;
        _roomCodeService = roomCodeService;
    }
    
    public void Start()
    {
        _steamService.JoinRequested += (lobby, _) =>
        {
            var roomCode = lobby.GetData(SteamService.RoomCodeKey);
            _roomCodeService.OnClientReceiveCode(roomCode);
        };
    }
}
