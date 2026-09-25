using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public sealed class PauseMenuLeaveHandler : IStartable, IDisposable
{
    private readonly PauseMenuScreen _screen;
    private readonly SettingsScreen _settings;
    private readonly Session _session;
    private readonly LoadingScreen _loadingScreen;
    private readonly PauseMenuToggle _pauseMenuToggle;
    private readonly ILobbyProvider _lobbyProvider;
    private bool _isLeaving;

    public bool IsLeaving => _isLeaving;

    public PauseMenuLeaveHandler(PauseMenuScreen screen, SettingsScreen settings, Session session,
        LoadingScreen loadingScreen, PauseMenuToggle pauseMenuToggle, ILobbyProvider lobbyProvider)
    {
        _screen = screen;
        _settings = settings;
        _session = session;
        _loadingScreen = loadingScreen;
        _pauseMenuToggle = pauseMenuToggle;
        _lobbyProvider = lobbyProvider;
    }

    public void Start() => _screen.Leave += OnLeave;
    public void Dispose() => _screen.Leave -= OnLeave;
    private void OnLeave() => ReturnToMenuAsync().Forget(Debug.LogException);

    public async UniTask ReturnToMenuAsync(bool reconnect = true)
    {
        var token = Application.exitCancellationToken;
        if (_isLeaving)
        {
            await UniTask.WaitUntil(() => !_isLeaving, cancellationToken: token);
            return;
        }

        _isLeaving = true;
        try
        {
            _pauseMenuToggle.ResetAndBlock();
            await _screen.Hide(instant: true);
            await _settings.Hide(instant: true);
            _loadingScreen.SetStatus(S.UI("leaving"));
            await _loadingScreen.SelectBar(LoadingScreen.BarType.LoadingBar, cancellationToken: token);
            await _loadingScreen.Show(instant: true, cancellationToken: token);
            // Let the Fusion shutdown callback unwind before loading/unloading scenes.
            await UniTask.NextFrame(cancellationToken: token);
            try { _lobbyProvider.LeaveLobby(); }
            catch (Exception exception) { Debug.LogException(exception); }
            try { await _session.LeaveAsync(); }
            catch (Exception exception) when (!token.IsCancellationRequested) { Debug.LogException(exception); }

            token.ThrowIfCancellationRequested();
            _loadingScreen.SetStatus(S.UI("loading"));
            await _loadingScreen.SelectBar(LoadingScreen.BarType.ProgressBar, cancellationToken: token);
            // Menu access must not depend on reconnecting to an unavailable server.
            // Host and Join reconnect when the player next starts a session.
            await _session.LoadMenu(token);
            if (reconnect)
            {
                // Restore the Photon identity used by the save list, but do not trap
                // the player behind the loading screen when their network is down.
                using var reconnectCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                reconnectCancellation.CancelAfterSlim(TimeSpan.FromSeconds(10));
                try
                {
                    _loadingScreen.SetStatus(S.UI("connecting"));
                    var connection = await _session.Connect(cancellationToken: reconnectCancellation.Token);
                    if (!connection.Ok) Debug.LogWarning($"Menu reconnect failed: {connection.ShutdownReason}");
                }
                catch (Exception exception) when (!token.IsCancellationRequested)
                {
                    Debug.LogWarning($"Menu opened without a network connection: {exception.Message}");
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally
        {
            try
            {
                if (!token.IsCancellationRequested && _loadingScreen != null)
                    await _loadingScreen.Hide(instant: true);
            }
            finally { _isLeaving = false; }
        }
    }
}
