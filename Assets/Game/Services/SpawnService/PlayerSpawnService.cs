using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;

public class PlayerSpawnService : INetworkRunnerCallbacks, IDisposable
{
    public interface IPlayerSpawnProvider
    {
        // Return null to leave the player queued until a spawn position is free.
        NetworkObject Spawn(NetworkRunner runner, PlayerRef player);
        void Despawn(NetworkRunner runner, PlayerRef player);
    }
    
    private IPlayerSpawnProvider _playerSpawnProvider;
    
    private readonly HashSet<PlayerRef> _joinedPlayers = new();
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    private NetworkRunner _runner;
    
    public void Attach(IPlayerSpawnProvider playerSpawnProvider)
    {
        if (playerSpawnProvider == null)
            throw new ArgumentNullException(nameof(playerSpawnProvider));
        if (ReferenceEquals(_playerSpawnProvider, playerSpawnProvider))
            return;

        Detach();
        _playerSpawnProvider = playerSpawnProvider;
    }

    public void Detach()
    {
        foreach (var player in new List<PlayerRef>(_spawnedPlayers.Keys))
            Despawn(player);

        _playerSpawnProvider = null;
    }

    public void Detach(IPlayerSpawnProvider provider)
    {
        // An unloading scene must not detach the next scene's provider.
        if (ReferenceEquals(_playerSpawnProvider, provider))
            Detach();
    }

    public void SpawnJoined()
    {
        if (!CanSpawn)
            return;

        foreach (var playerRef in new List<PlayerRef>(_joinedPlayers))
        {
            Spawn(playerRef);
        }
    }
    
    private bool CanSpawn => _runner != null && _runner.IsRunning &&
                             _runner.IsServer && !_runner.IsSceneManagerBusy &&
                             _playerSpawnProvider != null;

    private void Spawn(PlayerRef playerRef)
    {
        if (!CanSpawn || !_joinedPlayers.Contains(playerRef))
            return;
        if (_spawnedPlayers.TryGetValue(playerRef, out var existing) && existing != null)
            return;

        var networkObject = _playerSpawnProvider.Spawn(_runner, playerRef);
        if (networkObject == null)
            return;

        _spawnedPlayers[playerRef] = networkObject;
    }
    
    private void Despawn(PlayerRef player)
    {
        if (!_spawnedPlayers.ContainsKey(player))
            return;
        
        if (_runner != null && _runner.IsRunning && _runner.IsServer)
            _playerSpawnProvider?.Despawn(_runner, player);

        _spawnedPlayers.Remove(player);
    }
    
    public void AddCallbacks(NetworkRunner runner)
    {
        if (runner == null)
            throw new ArgumentNullException(nameof(runner));
        if (ReferenceEquals(_runner, runner))
            return;

        if (!ReferenceEquals(_runner, null))
        {
            Detach();
            ResetRunner();
        }

        _runner = runner;
        runner.AddCallbacks(this);
        
        if (runner.IsRunning && runner.IsServer)
        {
            foreach (var player in runner.ActivePlayers)
                _joinedPlayers.Add(player);
        }
    }

    public void Dispose()
    {
        try
        {
            Detach();
        }
        finally
        {
            ResetRunner();
        }
    }

    private void ResetRunner()
    {
        if (_runner != null)
            _runner.RemoveCallbacks(this);

        _runner = null;
        _playerSpawnProvider = null;
        _joinedPlayers.Clear();
        _spawnedPlayers.Clear();
    }
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!ReferenceEquals(_runner, runner) || !runner.IsServer)
            return;

        _joinedPlayers.Add(player);
        Spawn(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!ReferenceEquals(_runner, runner))
            return;

        _joinedPlayers.Remove(player);
        Despawn(player);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (ReferenceEquals(_runner, runner))
            ResetRunner();
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
        
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }
}
