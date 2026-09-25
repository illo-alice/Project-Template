using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public sealed class AfterSelectScreenPlayHandler : IStartable, IDisposable
{
    private readonly AfterSelectScreen _afterSelectScreen;
    private readonly LoadingScreen _loadingScreen;
    private readonly Session _session;
    private readonly ILobbyProvider _lobbyProvider;
    private readonly PauseMenuToggle _pauseMenuToggle;
    private readonly MessageBox _messageBox;
    private readonly GameSaveService _gameSaveService;
    private readonly SaveListUI _saveListUI;
    private bool _isPlaying;

    public AfterSelectScreenPlayHandler(
        AfterSelectScreen afterSelectScreen, LoadingScreen loadingScreen, 
        Session session, ILobbyProvider lobbyProvider,
        PauseMenuToggle pauseMenuToggle, MessageBox messageBox,
        GameSaveService gameSaveService, SaveListUI saveListUI)
    {
        _afterSelectScreen = afterSelectScreen;
        _loadingScreen = loadingScreen;
        _session = session;
        _lobbyProvider = lobbyProvider;
        _pauseMenuToggle = pauseMenuToggle;
        _messageBox = messageBox;
        _gameSaveService = gameSaveService;
        _saveListUI = saveListUI;
    }

    public void Start() => _afterSelectScreen.Play += OnPlay;
    public void Dispose() => _afterSelectScreen.Play -= OnPlay;

    private void OnPlay()
    {
        Play().Forget();
    }

    private async UniTask Play()
    {
        if (_isPlaying || _session.IsBusy || !_session.TryBeginTransition()) return;
        _isPlaying = true;
        var exitToken = Application.exitCancellationToken;
        SaveInfo createdSave = null;
        try
        {
            _afterSelectScreen.SetInteractable(false);
            _pauseMenuToggle.Block();
            var maxPlayers = _afterSelectScreen.MaxPlayers;
            var accessMode = _afterSelectScreen.AccessMode;
            var isNewGame = _saveListUI.IsNewGameSelected;
            var selectedSaveId = _saveListUI.SelectedSave?.Id;
            var saveName = _afterSelectScreen.SaveName;

            await _loadingScreen.SelectBar(LoadingScreen.BarType.LoadingBar, cancellationToken: exitToken);
            _loadingScreen.SetStatus(S.UI("creating_lobby"));
            await _loadingScreen.Show(cancellationToken: exitToken);
            
            if (!_session.IsConnected)
            {
                _loadingScreen.SetStatus(S.UI("connecting"));
                await _session.LeaveAsync();
                var connection = await _session.Connect(cancellationToken: exitToken);
                if (!connection.Ok)
                    throw new InvalidOperationException($"Connection failed: {connection.ShutdownReason}");
            }

            _loadingScreen.SetStatus(S.UI("creating_lobby"));
            await _lobbyProvider.CreateLobbyAsync(maxPlayers, accessMode);
            exitToken.ThrowIfCancellationRequested();
            _loadingScreen.SetStatus(S.UI("creating_session"));
            var result = await _session.HostLobby(await _lobbyProvider.GetSessionName(), maxPlayers,
                accessMode != LobbyAccessMode.Closed);
            exitToken.ThrowIfCancellationRequested();
            if (!result.Ok)
                throw new InvalidOperationException($"Hosting failed: {result.ShutdownReason}");

            if (isNewGame)
            {
                _loadingScreen.SetStatus(S.UI("creating_save"));
                createdSave = await _gameSaveService.Create(saveName, cancellationToken: exitToken);
            }
            else
            {
                _loadingScreen.SetStatus(S.UI("loading_save"));
                await _gameSaveService.Load(selectedSaveId, cancellationToken: exitToken);
            }
            
            _loadingScreen.SetStatus(S.UI("loading"));
            await _loadingScreen.SelectBar(LoadingScreen.BarType.ProgressBar, cancellationToken: exitToken);
            await _session.LoadLobby();
            _lobbyProvider.PublishSession();
            await _loadingScreen.Hide(cancellationToken: exitToken);
            _pauseMenuToggle.UnBlock();
        }
        catch (OperationCanceledException) when (exitToken.IsCancellationRequested)
        {
            await DeleteFailedSave(createdSave);
        }
        catch (Exception exception)
        {
            await DeleteFailedSave(createdSave);
            if (exitToken.IsCancellationRequested) return;
            Debug.LogException(exception);
            try
            {
                _lobbyProvider.LeaveLobby();
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }
            try
            {
                await _session.LeaveAsync();
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }

            if (exitToken.IsCancellationRequested) return;
            await _session.LoadMenu(exitToken);
            await _loadingScreen.Hide(instant: true, cancellationToken: exitToken);
            await _messageBox.Show(S.UI("connection_failed"), S.UI("lobby_create_failed_description"), S.UI("btn_understood"),
                cancellationToken: exitToken);
            // A scene-load failure may already have destroyed the menu screen.
            if (_afterSelectScreen != null)
                await _afterSelectScreen.Show();
            else
                await _session.LoadMenu(exitToken);
        }
        finally
        {
            try
            {
                if (!exitToken.IsCancellationRequested)
                {
                    if (_afterSelectScreen != null) _afterSelectScreen.SetInteractable(true);
                    if (_loadingScreen != null) await _loadingScreen.Hide(instant: true);
                }
            }
            finally
            {
                _isPlaying = false;
                _session.EndTransition();
            }
        }
    }

    private async UniTask DeleteFailedSave(SaveInfo createdSave)
    {
        if (createdSave == null) return;
        try
        {
            // Cleanup must finish even when application exit has cancelled the Play operation.
            await _gameSaveService.Delete(createdSave.Id, createdSave.PlayerId);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
