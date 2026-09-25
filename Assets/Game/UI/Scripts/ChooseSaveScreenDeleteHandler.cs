using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public sealed class ChooseSaveScreenDeleteHandler : IStartable, IDisposable
{
    private readonly ChooseSaveScreen _screen;
    private readonly SaveListUI _saveList;
    private readonly GameSaveService _saves;
    private readonly MessageBox _messageBox;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _isDeleting;

    public ChooseSaveScreenDeleteHandler(
        ChooseSaveScreen screen, SaveListUI saveList, GameSaveService saves, MessageBox messageBox)
    {
        _screen = screen;
        _saveList = saveList;
        _saves = saves;
        _messageBox = messageBox;
    }

    public void Start()
    {
        _screen.Delete += OnDelete;
        _saveList.SelectionChanged += UpdateDeleteButton;
        UpdateDeleteButton(_saveList.SelectedSave);
    }

    public void Dispose()
    {
        _screen.Delete -= OnDelete;
        _saveList.SelectionChanged -= UpdateDeleteButton;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private void UpdateDeleteButton(SaveInfo save)
    {
        if (_screen != null)
            _screen.SetDeleteInteractable(!_isDeleting && !_saveList.IsNewGameSelected && save != null);
    }

    private void OnDelete()
    {
        if (_isDeleting || _saveList.IsNewGameSelected || _saveList.SelectedSave == null) return;
        ConfirmDelete(_saveList.SelectedSave).Forget();
    }

    private async UniTask ConfirmDelete(SaveInfo save)
    {
        // Keep the exact save named in the prompt even if the list refreshes while it is open.
        var saveId = save.Id;
        var saveName = save.Name;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, Application.exitCancellationToken);
        var token = cancellation.Token;
        _isDeleting = true;
        _screen.SetInteractable(false);
        UpdateDeleteButton(save);
        try
        {
            var option = await _messageBox.Show(
                S.UI("save_delete_title"),
                string.Format(S.UI("save_delete_confirmation"), saveName),
                S.UI("btn_cancel"), S.UI("btn_delete"),
                cancellationToken: token);

            if (option == MessageBox.Option.Second)
                await _saves.Delete(saveId, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (!token.IsCancellationRequested)
                await _messageBox.Show(S.UI("error"), S.UI("save_delete_failed"), S.UI("btn_understood"), cancellationToken: token);
        }
        finally
        {
            _isDeleting = false;
            if (!token.IsCancellationRequested && _screen != null)
            {
                _screen.SetInteractable(true);
                UpdateDeleteButton(_saveList.SelectedSave);
            }
        }
    }
}
