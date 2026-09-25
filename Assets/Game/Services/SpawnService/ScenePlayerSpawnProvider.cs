using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

[DisallowMultipleComponent]
public sealed class ScenePlayerSpawnProvider : MonoBehaviour, PlayerSpawnService.IPlayerSpawnProvider
{
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints = Array.Empty<Transform>();
    [SerializeField, Min(0.1f)] private float _minimumPlayerDistance = 2f;
    [SerializeField, Min(0.1f)] private float _clearanceRadius = 0.5f;
    [SerializeField, Min(0.2f)] private float _clearanceHeight = 1.8f;
    [SerializeField] private LayerMask _blockingLayers = ~0;

    private readonly Dictionary<PlayerRef, SpawnedPlayer> _players = new();
    private readonly Collider[] _overlaps = new Collider[1];
    private PlayerSpawnService _service;
    private NetworkRunner _runner;
    private bool _attached;
    private bool _waitingForPosition;
    private float _nextRetry;

    private readonly struct SpawnedPlayer
    {
        public readonly NetworkObject Object;
        public readonly int PointIndex;

        public SpawnedPlayer(NetworkObject obj, int pointIndex)
        {
            Object = obj;
            PointIndex = pointIndex;
        }
    }

    [Inject]
    public void Construct(PlayerSpawnService service, Session session)
    {
        _service = service;
        _runner = session.Runner;
    }

    private void Update()
    {
        // LifetimeScope can inject while Fusion is still loading this scene.
        if (_service == null || _runner == null || !_runner.IsRunning || !_runner.IsServer)
            return;

        if (_runner.IsSceneManagerBusy)
        {
            _waitingForPosition = true;
            return;
        }

        if (!_attached)
        {
            if (_playerPrefab == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("Assign a player prefab and spawn points to the scene spawn provider.", this);
                enabled = false;
                return;
            }

            _service.Attach(this);
            _attached = true;
            _waitingForPosition = true;
        }

        if (!_waitingForPosition || Time.unscaledTime < _nextRetry)
            return;

        _waitingForPosition = false;
        _nextRetry = Time.unscaledTime + 0.25f;
        _service.SpawnJoined();
    }

    private void OnDisable()
    {
        if (_attached)
            _service.Detach(this);

        _attached = false;
        _waitingForPosition = false;
        _nextRetry = 0f;
        _players.Clear();
    }

    public NetworkObject Spawn(NetworkRunner runner, PlayerRef player)
    {
        if (runner != _runner || !runner.IsRunning || !runner.IsServer)
            throw new InvalidOperationException("Players must be spawned by this scene's server runner.");

        if (_players.TryGetValue(player, out var existing) && existing.Object != null && existing.Object.IsValid)
            return existing.Object;

        _players.Remove(player);
        if (runner.IsSceneManagerBusy)
        {
            _waitingForPosition = true;
            return null;
        }

        // Newly moved colliders must be reflected in the occupancy query.
        Physics.SyncTransforms();
        for (var i = 0; i < _spawnPoints.Length; i++)
        {
            var point = _spawnPoints[i];
            if (point == null || !IsPointFree(runner, i, point.position))
                continue;

            var obj = runner.Spawn(_playerPrefab, point.position, point.rotation, inputAuthority: player);
            if (obj == null)
            {
                _waitingForPosition = true;
                return null;
            }

            try
            {
                runner.SetPlayerObject(player, obj);
                _players.Add(player, new SpawnedPlayer(obj, i));
                return obj;
            }
            catch
            {
                runner.Despawn(obj);
                throw;
            }
        }

        // Never fall back to an occupied point. Update retries pending players.
        _waitingForPosition = true;
        return null;
    }

    public void Despawn(NetworkRunner runner, PlayerRef player)
    {
        if (!_players.TryGetValue(player, out var spawned))
            return;

        if (runner != _runner)
            throw new InvalidOperationException("Cannot despawn a player through a different runner.");

        if (spawned.Object != null && spawned.Object.IsValid && runner.IsRunning && runner.IsServer)
            runner.Despawn(spawned.Object);

        _players.Remove(player);
    }

    private bool IsPointFree(NetworkRunner runner, int index, Vector3 position)
    {
        var distanceSquared = _minimumPlayerDistance * _minimumPlayerDistance;
        foreach (var spawned in _players.Values)
        {
            if (spawned.Object == null || !spawned.Object.IsValid)
                continue;

            if (spawned.PointIndex == index)
                return false;

            // Also works for the current player prefab, which has no collider.
            var offset = spawned.Object.transform.position - position;
            offset.y = 0f;
            if (offset.sqrMagnitude < distanceSquared)
                return false;
        }

        var physicsScene = gameObject.scene.GetPhysicsScene();
        if (runner.SceneManager != null && runner.SceneManager.TryGetPhysicsScene3D(out var runnerPhysicsScene))
            physicsScene = runnerPhysicsScene;

        var radius = Mathf.Max(0.1f, _clearanceRadius);
        var height = Mathf.Max(_clearanceHeight, radius * 2f);
        // Spawn transforms mark the feet. Leave a small gap above the floor.
        var bottom = position + Vector3.up * (radius + 0.05f);
        var top = position + Vector3.up * (height - radius + 0.05f);
        return physicsScene.OverlapCapsule(bottom, top, radius, _overlaps,
            _blockingLayers, QueryTriggerInteraction.Ignore) == 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (_spawnPoints == null)
            return;

        Gizmos.color = Color.cyan;
        foreach (var point in _spawnPoints)
        {
            if (point == null)
                continue;
            Gizmos.DrawWireSphere(point.position, _minimumPlayerDistance * 0.5f);
            Gizmos.DrawLine(point.position, point.position + point.forward);
        }
    }
}
