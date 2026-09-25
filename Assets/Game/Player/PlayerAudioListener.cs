using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Uses the local player's listening position and restores the menu listener on despawn.
/// The child AudioListener must be disabled in the prefab so remote players stay silent.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class PlayerAudioListener : NetworkBehaviour
{
    [SerializeField] private AudioListener _listener;

    private static PlayerAudioListener _owner;

    private readonly List<AudioListener> _fallbackListeners = new();
    private bool _spawned;

    private bool IsLocalPlayer => Runner.Topology == Topologies.Shared ? HasStateAuthority : HasInputAuthority;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOwner()
    {
        _owner = null;
    }

    private void Awake()
    {
        if (_listener != null)
            _listener.enabled = false;
    }

    public override void Spawned()
    {
        _spawned = true;
        RefreshOwnership();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _spawned = false;
        ReleaseListener();
    }

    private void OnEnable()
    {
        RefreshOwnership();
    }

    private void LateUpdate()
    {
        // Authority can change after Spawned (or this component can be re-enabled).
        RefreshOwnership();
    }

    private void OnDisable()
    {
        ReleaseListener();
    }

    private void OnDestroy()
    {
        ReleaseListener();
    }

    private void RefreshOwnership()
    {
        if (!_spawned || !isActiveAndEnabled || Object == null || !Object.IsValid || Runner == null ||
            !IsLocalPlayer || _listener == null || !_listener.gameObject.activeInHierarchy)
        {
            ReleaseListener();
            return;
        }

        if (_owner != null)
            return;

        _owner = this;
        DisableFallbackListeners();
        _listener.enabled = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Keep additive scene cameras from introducing a second active listener.
        DisableFallbackListeners();
    }

    private void DisableFallbackListeners()
    {
        foreach (var listener in FindObjectsByType<AudioListener>())
        {
            if (listener == _listener || !listener.isActiveAndEnabled)
                continue;

            if (!_fallbackListeners.Contains(listener))
                _fallbackListeners.Add(listener);

            listener.enabled = false;
        }
    }

    private void ReleaseListener()
    {
        if (_listener != null)
            _listener.enabled = false;

        if (_owner != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        _owner = null;

        foreach (var listener in _fallbackListeners)
        {
            if (listener != null)
                listener.enabled = true;
        }

        _fallbackListeners.Clear();
    }
}
