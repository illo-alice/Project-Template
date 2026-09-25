using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public class Session : INetworkRunnerCallbacks
{
    private enum SessionState
    {
        Disconnected,
        Connecting,
        Connected,
        Starting,
        Started,
        Stopping
    }

    public SessionObjectProvider ObjectProvider => _objectProvider;
    public bool IsConnected => _state == SessionState.Connected;
    public bool IsStarted => _state == SessionState.Started;
    public bool IsTransitioning { get; private set; }
    public bool TryBeginTransition()
    {
        if (IsTransitioning) return false;
        IsTransitioning = true;
        return true;
    }
    public void EndTransition()
    {
        IsTransitioning = false;
        var reason = _pendingConnectionLost;
        _pendingConnectionLost = null;
        if (reason.HasValue && _state == SessionState.Disconnected && !_exitToken.IsCancellationRequested)
            ConnectionLost?.Invoke(reason.Value);
    }
    public bool IsBusy => _operationInProgress || _isLeaving || _stopInProgress || _isJoiningLobby;
    public string Region => _runner == null ? null :
        (_state == SessionState.Started ? _runner.SessionInfo.Region : _runner.LobbyInfo.Region);
    public string PlayerId { get; private set; }
    public NetworkRunner Runner
    {
        get
        {
            if (_runner == null) 
                throw new NullReferenceException(nameof(_runner) + "is not assigned");
            
            return _runner;
        }
    }
    private GameObject _sessionObject;
    private SessionObjectProvider _objectProvider;
    private SessionSceneManager _sceneManager;
    private NetworkRunner _runner;
    private readonly RegionParameter _region;
    private readonly VoiceChatService _voice;
    private readonly PlayerSpawnService _playerSpawnService;
    private IAuthenticationProvider _authenticationProvider;
    private IObjectResolver _objectResolver;
    private SessionNetworkProxy _networkProxy;
    private SessionState _state;
    private bool _operationInProgress;
    private bool _isLeaving;
    private bool _stopInProgress;
    private bool _isJoiningLobby;
    private ShutdownReason? _pendingConnectionLost;
    private readonly CancellationToken _exitToken = Application.exitCancellationToken;
    private readonly SemaphoreSlim _menuLoadGate = new(1, 1);
    private AsyncOperation _menuLoadOperation;
    public event Action SceneLoadStarted;
    public event Action<float> SceneLoadProgressChanged;
    public event Action SceneLoadCompleted;
    public event Action<ShutdownReason> Shutdown;
    public event Action<ShutdownReason> ConnectionLost;
    public event Action PlayerIdChanged;

    private void HandleSceneLoadProgress(float progress)
    {
        SceneLoadProgressChanged?.Invoke(progress);
    }
    
    public Session(IAuthenticationProvider authenticationProvider, SessionNetworkProxy networkProxy, RegionParameter region, VoiceChatService voice, IObjectResolver objectResolver, PlayerSpawnService playerSpawnService)
    {
        _authenticationProvider = authenticationProvider;
        _networkProxy = networkProxy;
        _region = region;
        _voice = voice;
        _objectResolver = objectResolver;
        _playerSpawnService = playerSpawnService;
    }

    public async UniTask<StartGameResult> Connect(string region = null, CancellationToken cancellationToken = default)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _exitToken);
        cancellationToken = cancellation.Token;
        cancellationToken.ThrowIfCancellationRequested();
        if (_operationInProgress || _isLeaving || _state != SessionState.Disconnected)
            throw new InvalidOperationException($"Cannot connect while session is {_state}.");

        cancellationToken.ThrowIfCancellationRequested();
        _operationInProgress = true;
        _state = SessionState.Connecting;
        _pendingConnectionLost = null;

        try
        {
            _sessionObject = new GameObject("Session");
            UnityEngine.Object.DontDestroyOnLoad(_sessionObject);
            _runner = _sessionObject.AddComponent<NetworkRunner>();
            _runner.AddCallbacks(this);
            _playerSpawnService.AddCallbacks(_runner);

            var auth = await _authenticationProvider.GetAuth().AttachExternalCancellation(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();

            var settings = PhotonAppSettings.Global.AppSettings.GetCopy();

            settings.FixedRegion = region ?? _region.FixedRegion ?? string.Empty;
            
            var result = await _runner.JoinSessionLobby(
                SessionLobby.ClientServer,
                authentication: auth,
                customAppSettings: settings,
                cancellationToken: cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (result.Ok)
            {
                ThrowIfShutdown();
                _state = SessionState.Connected;
                SetPlayerId(_runner.UserId);
            }
            else
            {
                await Stop();
            }

            return result;
        }
        catch
        {
            await Stop();
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        finally
        {
            _operationInProgress = false;
        }
    }
    
    private async UniTask<StartGameResult> StartSession(
        GameMode mode,
        string roomCode,
        int? maxPlayers = null,
        bool isOpen = true,
        CancellationToken cancellationToken = default)
    {
        if (_operationInProgress || _isLeaving || _state != SessionState.Connected)
            throw new InvalidOperationException($"Cannot start a session while it is {_state}. Complete Connect first.");

        cancellationToken.ThrowIfCancellationRequested();
        _operationInProgress = true;
        _state = SessionState.Starting;

        try
        {
            _sceneManager = _sessionObject.AddComponent<SessionSceneManager>();
            _sceneManager.ProgressChanged += HandleSceneLoadProgress;

            _objectProvider = _sessionObject.AddComponent<SessionObjectProvider>();
            _objectProvider.SetObjectResolver(_objectResolver);
            _voice.Attach(_runner);
            
            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SceneManager = _sceneManager,
                ObjectProvider = _objectProvider,
                SessionName = roomCode,
                PlayerCount = maxPlayers,
                IsOpen = isOpen,
                IsVisible = false,
                EnableClientSessionCreation = false,
                StartGameCancellationToken = cancellationToken,
            });

            if (result.Ok)
            {
                ThrowIfShutdown();
                _state = SessionState.Started;
            }
            else
            {
                await Stop();
            }

            return result;
        }
        catch
        {
            await Stop();
            throw;
        }
        finally
        {
            _operationInProgress = false;
        }
    }

    public async UniTask LoadMenu(CancellationToken cancellationToken = default)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _exitToken);
        cancellationToken = cancellation.Token;
        await _menuLoadGate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var menu = SceneManager.GetSceneByName("Menu");
            if (menu.IsValid() && menu.isLoaded)
            {
                SceneManager.SetActiveScene(menu);
                await UnloadGameplayScenes(cancellationToken);
                return;
            }

            SceneLoadStarted?.Invoke();
            SceneLoadProgressChanged?.Invoke(0f);
            cancellationToken.ThrowIfCancellationRequested();

            // Unity cannot abort a scene load. Reuse an in-flight operation if
            // its previous caller stopped waiting, instead of loading a second copy.
            if (_menuLoadOperation == null || _menuLoadOperation.isDone)
                _menuLoadOperation = SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);

            var progress = Progress.CreateOnlyValueChanged<float>(HandleSceneLoadProgress);
            await _menuLoadOperation.ToUniTask(progress: progress, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _menuLoadOperation = null;

            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Menu"));
            await UnloadGameplayScenes(cancellationToken);
            SceneLoadProgressChanged?.Invoke(1f);
            SceneLoadCompleted?.Invoke();
        }
        finally
        {
            _menuLoadGate.Release();
        }
    }
    
    public bool TryGetPing(out int milliseconds)
    {
        milliseconds = 0;
        if (_runner == null || !_runner.IsRunning || _runner.GameMode == GameMode.Single) return false;
        milliseconds = Mathf.RoundToInt((float)_runner.GetPlayerRtt(_runner.LocalPlayer) * 1000f);
        return true;
    }

    public async UniTask<StartGameResult> HostLobby(string roomCode, int? maxPlayers = null, bool isOpen = true)
    {
        return await StartSession(GameMode.Host, roomCode, maxPlayers, isOpen, _exitToken);
    }

    public async UniTask<StartGameResult> JoinLobby(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
            throw new ArgumentException("A room code is required.", nameof(roomCode));
        if (_isJoiningLobby) throw new InvalidOperationException("Already joining a lobby.");
        _isJoiningLobby = true;
        try
        {
            var result = await StartSession(GameMode.Client, roomCode.Trim(), cancellationToken: _exitToken);
            if (!result.Ok) return result;
            // StartGame may finish before the host's additive scene is ready.
            await UniTask.WaitUntil(() => !IsStarted || IsGameplaySceneReady(), cancellationToken: _exitToken)
                .Timeout(TimeSpan.FromSeconds(45));
            if (!IsStarted) throw new InvalidOperationException("Disconnected while loading the lobby.");
            var menu = SceneManager.GetSceneByName("Menu");
            if (menu.IsValid() && menu.isLoaded) await SceneManager.UnloadSceneAsync(menu);
            return result;
        }
        finally { _isJoiningLobby = false; }
    }

    private bool IsGameplaySceneReady()
    {
        if (_sceneManager == null || _sceneManager.IsBusy) return false;
        var lobby = SceneManager.GetSceneByName("Lobby");
        var game = SceneManager.GetSceneByName("Game");
        return (lobby.IsValid() && lobby.isLoaded) || (game.IsValid() && game.isLoaded);
    }

    private static async UniTask UnloadGameplayScenes(CancellationToken cancellationToken)
    {
        foreach (var name in new[] { "Lobby", "Game" })
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scene = SceneManager.GetSceneByName(name);
            if (scene.IsValid() && scene.isLoaded)
                await SceneManager.UnloadSceneAsync(scene).ToUniTask(cancellationToken: cancellationToken);
        }
    }

    public async UniTask LoadLobby()
    {
        await LoadScene(2, LoadSceneMode.Additive);
        await SceneManager.UnloadSceneAsync("Menu");
    }
    
    public void LoadGame()
    {
        _networkProxy.RPC_LoadScene(3);
    }

    public async UniTask LeaveAsync()
    {
        if (_isLeaving)
        {
            await UniTask.WaitUntil(() => !_isLeaving, cancellationToken: _exitToken);
            return;
        }

        _isLeaving = true;
        try
        {
            if (_operationInProgress)
                await UniTask.WaitUntil(() => !_operationInProgress, cancellationToken: _exitToken);

            await Stop();
        }
        finally
        {
            _isLeaving = false;
        }
    }

    private async UniTask LoadScene(int index, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (!_runner.IsSceneAuthority)
            throw new InvalidOperationException("is not authorized for load scene");
        
        await _runner.LoadScene(
            SceneRef.FromIndex(index),
            mode,
            setActiveOnLoad: true);
    }

    private async UniTask UnloadScene(int index)
    {
        if (!_runner.IsSceneAuthority)
            throw new InvalidOperationException("is not authorized for unload scene");
        
        await _runner.UnloadScene(SceneRef.FromIndex(index));
    }
    
    private async UniTask Stop()
    {
        if (_stopInProgress)
        {
            await UniTask.WaitUntil(() => !_stopInProgress, cancellationToken: _exitToken);
            return;
        }
        if (_exitToken.IsCancellationRequested)
        {
            // Fusion handles application shutdown; do not start another async
            // shutdown against objects Unity is already destroying.
            ResetSession();
            return;
        }

        _stopInProgress = true;
        _state = SessionState.Stopping;
        var runner = _runner;
        var sessionObject = _sessionObject;

        try
        {
            await _voice.StopAsync();
            if (runner != null)
                await runner.Shutdown(destroyGameObject: true);
            else if (sessionObject != null)
                UnityEngine.Object.Destroy(sessionObject);
        }
        finally
        {
            ResetSession();
            _stopInProgress = false;
        }
    }

    private void ThrowIfShutdown()
    {
        if (_state == SessionState.Disconnected)
            throw new OperationCanceledException("The session was closed before the operation completed.");
    }

    private void ResetSession()
    {
        _voice.Detach();

        if (_sceneManager != null)
            _sceneManager.ProgressChanged -= HandleSceneLoadProgress;

        if (_runner != null)
            _runner.RemoveCallbacks(this);

        _runner = null;
        _sessionObject = null;
        _sceneManager = null;
        _objectProvider = null;
        _state = SessionState.Disconnected;
        SetPlayerId(null);
    }

    private void SetPlayerId(string playerId)
    {
        if (string.Equals(PlayerId, playerId, StringComparison.Ordinal)) return;
        PlayerId = playerId;
        PlayerIdChanged?.Invoke();
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (runner != _runner || _state == SessionState.Disconnected)
            return;

        if (_stopInProgress) return; // Stop owns cleanup until Shutdown's task has completed.
        var lostSession = _state == SessionState.Started && !_isLeaving;
        ResetSession();
        Shutdown?.Invoke(shutdownReason);
        if (lostSession && !_exitToken.IsCancellationRequested)
        {
            if (IsTransitioning || _isJoiningLobby) _pendingConnectionLost = shutdownReason;
            else ConnectionLost?.Invoke(shutdownReason);
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
    {
        
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        SceneLoadProgressChanged?.Invoke(1f);
        SceneLoadCompleted?.Invoke();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        SceneLoadStarted?.Invoke();
        SceneLoadProgressChanged?.Invoke(0f);
    }
}
