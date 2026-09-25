using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using VContainer.Unity;

public class SteamService : ITickable, IDisposable, ILobbyProvider
{
    public const uint AppId = 480;
    public const string RoomCodeKey = "room_code";
    public const string RegionKey = "photon_region";
    public const string GameKey = "game";
    public const string GameIdentity = "friendslop_template_v1";
    public const string ReadyKey = "ready";
    public const string AccessKey = "access";

    public event Action<Lobby, SteamId> JoinRequested;
    public event Action LobbyChanged;
    public Lobby? CurrentLobby { get; private set; }
    public bool CanInvite => _initialized && CurrentLobby.HasValue && _session.IsStarted &&
        _accessMode != LobbyAccessMode.Closed && CurrentLobby.Value.GetData(ReadyKey) == "1";

    private readonly RoomCodeService _roomCodeService;
    private readonly Session _session;
    private bool _initialized;
    private bool _joinRequestsEnabled;
    private Lobby? _pendingJoin;
    private SteamId _pendingFriend;
    private LobbyAccessMode _accessMode = LobbyAccessMode.Closed;

    public SteamService(RoomCodeService roomCodeService, Session session)
    {
        _roomCodeService = roomCodeService;
        _session = session;
    }

    public void Initialize()
    {
        if (_initialized) return;
        SteamClient.Init(AppId, asyncCallbacks: false);
        if (!SteamClient.IsValid) throw new InvalidOperationException("Steam initialization failed");
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
        SteamFriends.OnGameRichPresenceJoinRequested += OnRichPresenceJoinRequested;
        _initialized = true;
        if (TryParseLobbyId(string.Join(" ", Environment.GetCommandLineArgs()), out var lobbyId))
            OnGameLobbyJoinRequested(new Lobby(lobbyId), default);
    }

    public void EnableJoinRequests()
    {
        _joinRequestsEnabled = true;
        if (!_pendingJoin.HasValue) return;
        var lobby = _pendingJoin.Value;
        _pendingJoin = null;
        JoinRequested?.Invoke(lobby, _pendingFriend);
    }

    public void Tick()
    {
        if (_initialized) SteamClient.RunCallbacks();
    }

    public void Dispose()
    {
        if (!_initialized) return;
        LeaveLobby();
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        SteamFriends.OnGameRichPresenceJoinRequested -= OnRichPresenceJoinRequested;
        SteamClient.Shutdown();
        _initialized = false;
        _joinRequestsEnabled = false;
        _pendingJoin = null;
    }

    public async UniTask<Lobby> CreateLobbyAsync(int maxPlayers, LobbyAccessMode accessMode)
    {
        EnsureSteam();
        if (!Enum.IsDefined(typeof(LobbyAccessMode), accessMode))
            throw new ArgumentOutOfRangeException(nameof(accessMode));
        var region = _session.Region;
        if (string.IsNullOrWhiteSpace(region) || region == "auto")
            throw new InvalidOperationException("Connect to a Photon region before creating a Steam lobby.");
        LeaveLobby();
        var result = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
        if (!result.HasValue) throw new InvalidOperationException("Steam lobby creation failed");
        var lobby = result.Value;
        try
        {
            if (!lobby.SetJoinable(false) ||
                !lobby.SetData(GameKey, GameIdentity) ||
                !lobby.SetData(ReadyKey, "0") ||
                !lobby.SetData(RoomCodeKey, _roomCodeService.Generate()) ||
                !lobby.SetData(RegionKey, region) ||
                !lobby.SetData(AccessKey, accessMode.ToString()) ||
                !(accessMode == LobbyAccessMode.FriendsOnly ? lobby.SetFriendsOnly() : lobby.SetPrivate()))
                throw new InvalidOperationException("Could not configure Steam lobby");
            CurrentLobby = lobby;
            _accessMode = accessMode;
            LobbyChanged?.Invoke();
            return lobby;
        }
        catch { lobby.Leave(); throw; }
    }

    public void PublishSession()
    {
        if (!CurrentLobby.HasValue || !_session.IsStarted)
            throw new InvalidOperationException("The game session is not ready.");
        var lobby = CurrentLobby.Value;
        if (!lobby.SetData(RegionKey, _session.Region) || !lobby.SetData(ReadyKey, "1") ||
            !lobby.SetJoinable(_accessMode != LobbyAccessMode.Closed))
            throw new InvalidOperationException("Could not publish Steam lobby.");
        UpdatePresence();
        LobbyChanged?.Invoke();
    }

    public async UniTask<Lobby> JoinLobbyAsync(Lobby lobby)
    {
        EnsureSteam();
        var result = await lobby.Join();
        if (result != RoomEnter.Success)
            throw new InvalidOperationException($"Steam lobby join failed: {result}");
        try
        {
            // Metadata is guaranteed to be available only after entering the lobby.
            if (lobby.GetData(GameKey) != GameIdentity || lobby.GetData(ReadyKey) != "1" ||
                string.IsNullOrWhiteSpace(lobby.GetData(RoomCodeKey)) ||
                string.IsNullOrWhiteSpace(lobby.GetData(RegionKey)) || lobby.GetData(RegionKey) == "auto" ||
                !Enum.TryParse(lobby.GetData(AccessKey), out LobbyAccessMode access) || access == LobbyAccessMode.Closed)
                throw new InvalidOperationException("This lobby is unavailable or belongs to another game/build.");
            CurrentLobby = lobby;
            _accessMode = access;
            _roomCodeService.OnClientReceiveCode(lobby.GetData(RoomCodeKey));
            return lobby;
        }
        catch { lobby.Leave(); throw; }
    }

    public void RefreshPresence()
    {
        UpdatePresence();
        LobbyChanged?.Invoke();
    }

    private void UpdatePresence()
    {
        if (!_initialized) return;
        SteamFriends.ClearRichPresence();
        if (!CanInvite) return;
        var id = CurrentLobby.Value.Id.ToString();
        SteamFriends.SetRichPresence("connect", $"+connect_lobby {id}");
        SteamFriends.SetRichPresence("steam_player_group", id);
        SteamFriends.SetRichPresence("steam_player_group_size", CurrentLobby.Value.MemberCount.ToString(CultureInfo.InvariantCulture));
    }

    public void LeaveLobby()
    {
        if (_initialized && CurrentLobby.HasValue)
        {
            var lobby = CurrentLobby.Value;
            // Steam migrates lobby ownership; the Photon host does not migrate.
            if (lobby.Owner.Id == SteamClient.SteamId)
            {
                lobby.SetData(ReadyKey, "0");
                lobby.SetJoinable(false);
            }
            lobby.Leave();
        }
        CurrentLobby = null;
        _accessMode = LobbyAccessMode.Closed;
        _roomCodeService.OnClientReceiveCode(null);
        if (_initialized) SteamFriends.ClearRichPresence();
        LobbyChanged?.Invoke();
    }

    public UniTask<string> GetSessionName()
    {
        if (CurrentLobby.HasValue) return UniTask.FromResult(CurrentLobby.Value.GetData(RoomCodeKey));
        throw new InvalidOperationException("Steam lobby is not created");
    }

    public void OpenInviteOverlay()
    {
        if (!CanInvite) return;
        UpdatePresence();
        SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
    }

    private void EnsureSteam()
    {
        if (!_initialized || !SteamClient.IsValid || !SteamClient.IsLoggedOn)
            throw new InvalidOperationException("Steam client is not logged in");
    }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
    {
        if (!_joinRequestsEnabled)
        {
            _pendingJoin = lobby;
            _pendingFriend = friendId;
            return;
        }
        JoinRequested?.Invoke(lobby, friendId);
    }

    private void OnRichPresenceJoinRequested(Friend friend, string connect)
    {
        if (TryParseLobbyId(connect, out var lobbyId))
            OnGameLobbyJoinRequested(new Lobby(lobbyId), friend.Id);
    }

    public static bool TryParseLobbyId(string connect, out SteamId lobbyId)
    {
        lobbyId = default;
        if (string.IsNullOrWhiteSpace(connect)) return false;
        var parts = connect.Split(new[] { ' ', '\t', '\r', '\n', '"' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i + 1 < parts.Length; i++)
        {
            if (parts[i] == "+connect_lobby" && ulong.TryParse(parts[i + 1], NumberStyles.None,
                    CultureInfo.InvariantCulture, out var id) && id != 0)
            {
                lobbyId = id;
                return true;
            }
        }
        return false;
    }
}
