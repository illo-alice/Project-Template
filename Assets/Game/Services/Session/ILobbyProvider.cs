using Cysharp.Threading.Tasks;
using Steamworks.Data;

public interface ILobbyProvider
{
    public UniTask<Lobby> CreateLobbyAsync(int maxPlayers, LobbyAccessMode accessMode);
    public UniTask<string> GetSessionName();
    public void LeaveLobby();
    public void PublishSession();
}
