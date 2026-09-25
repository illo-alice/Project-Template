using System;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using VContainer.Unity;

public sealed class PauseMenuDisconnectHandler : IStartable, IDisposable
{
    private readonly Session _session;
    private readonly PauseMenuLeaveHandler _leaveHandler;

    public PauseMenuDisconnectHandler(Session session, PauseMenuLeaveHandler leaveHandler)
    {
        _session = session;
        _leaveHandler = leaveHandler;
    }

    public void Start() => _session.ConnectionLost += OnConnectionLost;
    public void Dispose() => _session.ConnectionLost -= OnConnectionLost;

    private void OnConnectionLost(ShutdownReason reason)
    {
        Debug.LogWarning($"Session ended ({reason}); returning to the main menu.");
        _leaveHandler.ReturnToMenuAsync().Forget(Debug.LogException);
    }
}
