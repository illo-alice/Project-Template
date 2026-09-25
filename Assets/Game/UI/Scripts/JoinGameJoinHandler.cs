using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public sealed class JoinGameJoinHandler : IStartable, IDisposable
{
    private readonly JoinScreen _joinScreen;
    private readonly Session _session;
    private readonly MessageBox _messageBox;
    private readonly LoadingScreen _loading;
    private readonly RoomCodeService _codes;
    private readonly PauseMenuToggle _pause;
    private readonly PauseMenuLeaveHandler _leave;

    public JoinGameJoinHandler(JoinScreen joinScreen, Session session, MessageBox messageBox,
        LoadingScreen loading, RoomCodeService codes, PauseMenuToggle pause, PauseMenuLeaveHandler leave)
    {
        _joinScreen = joinScreen;
        _session = session;
        _messageBox = messageBox;
        _loading = loading;
        _codes = codes;
        _pause = pause;
        _leave = leave;
    }

    public void Start() => _joinScreen.Join += Join;
    public void Dispose() => _joinScreen.Join -= Join;

    private void Join(string code)
    {
        if (_leave.IsLeaving || _session.IsBusy || string.IsNullOrWhiteSpace(code) || !_session.TryBeginTransition()) return;
        JoinAsync(code.Trim()).Forget(Debug.LogException);
    }

    private async UniTask JoinAsync(string code)
    {
        var token = Application.exitCancellationToken;
        try
        {
            _joinScreen.SetInteractable(false);
            _pause.ResetAndBlock();
            _loading.SetStatus(S.UI("connecting"));
            await _loading.SelectBar(LoadingScreen.BarType.LoadingBar, cancellationToken: token);
            await _loading.Show(cancellationToken: token);
            if (!_session.IsConnected)
            {
                var connection = await _session.Connect(cancellationToken: token);
                if (!connection.Ok) throw new InvalidOperationException($"Connection failed: {connection.ShutdownReason}");
            }
            _loading.SetStatus(S.UI("loading"));
            var result = await _session.JoinLobby(code);
            if (!result.Ok) throw new InvalidOperationException($"Join failed: {result.ShutdownReason}");
            _codes.OnClientReceiveCode(code);
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
            try
            {
                if (!token.IsCancellationRequested)
                {
                    if (_joinScreen != null) _joinScreen.SetInteractable(true);
                    await _loading.Hide(instant: true);
                }
            }
            finally { _session.EndTransition(); }
        }
    }
}
