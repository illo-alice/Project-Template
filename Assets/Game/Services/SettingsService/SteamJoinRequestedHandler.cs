using System;
using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using VContainer.Unity;

public sealed class SteamJoinRequestedHandler : IStartable, IDisposable
{
    private readonly SteamService _steam;
    private readonly Session _session;
    private readonly LoadingScreen _loading;
    private readonly PauseMenuToggle _pause;
    private readonly PauseMenuLeaveHandler _leave;
    private readonly MessageBox _messageBox;

    public SteamJoinRequestedHandler(SteamService steamService, Session session, LoadingScreen loadingScreen,
        PauseMenuToggle pause, PauseMenuLeaveHandler leave, MessageBox messageBox)
    {
        _steam = steamService;
        _session = session;
        _loading = loadingScreen;
        _pause = pause;
        _leave = leave;
        _messageBox = messageBox;
    }

    public void Start() => _steam.JoinRequested += OnJoinRequested;
    public void Dispose() => _steam.JoinRequested -= OnJoinRequested;

    private void OnJoinRequested(Lobby lobby, SteamId friend)
    {
        if (_leave.IsLeaving || _session.IsBusy ||
            (_session.IsStarted && _steam.CurrentLobby?.Id == lobby.Id) || !_session.TryBeginTransition()) return;
        JoinAsync(lobby).Forget(Debug.LogException);
    }

    private async UniTask JoinAsync(Lobby lobby)
    {
        var token = Application.exitCancellationToken;
        try
        {
            await _leave.ReturnToMenuAsync(reconnect: false);
            token.ThrowIfCancellationRequested();
            _loading.SetStatus(S.UI("connecting"));
            await _loading.SelectBar(LoadingScreen.BarType.LoadingBar, cancellationToken: token);
            await _loading.Show(cancellationToken: token);
            lobby = await _steam.JoinLobbyAsync(lobby);
            token.ThrowIfCancellationRequested();
            var connection = await _session.Connect(lobby.GetData(SteamService.RegionKey), token);
            if (!connection.Ok) throw new InvalidOperationException($"Connection failed: {connection.ShutdownReason}");
            _loading.SetStatus(S.UI("loading"));
            var result = await _session.JoinLobby(lobby.GetData(SteamService.RoomCodeKey));
            if (!result.Ok) throw new InvalidOperationException($"Join failed: {result.ShutdownReason}");
            token.ThrowIfCancellationRequested();
            _steam.RefreshPresence();
            _pause.UnBlock();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (token.IsCancellationRequested) return;
            Debug.LogException(exception);
            await _leave.ReturnToMenuAsync();
            await _messageBox.Show(S.UI("connection_failed"), S.UI("join_failed"), S.UI("btn_understood"),
                cancellationToken: token);
        }
        finally
        {
            try { if (!token.IsCancellationRequested) await _loading.Hide(instant: true); }
            finally { _session.EndTransition(); }
        }
    }
}
