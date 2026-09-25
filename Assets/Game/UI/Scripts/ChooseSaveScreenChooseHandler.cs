using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class ChooseSaveScreenChooseHandler : IStartable, IDisposable
{
    private readonly ChooseSaveScreen _chooseSaveScreen;
    private readonly SaveListUI _saveListUI;
    private readonly AfterSelectScreen _afterSelectScreen;
    private bool _isTransitioning;
    
    public ChooseSaveScreenChooseHandler(
        ChooseSaveScreen chooseSaveScreen, SaveListUI saveListUI, AfterSelectScreen afterSelectScreen)
    {
        _chooseSaveScreen = chooseSaveScreen;
        _saveListUI = saveListUI;
        _afterSelectScreen = afterSelectScreen;
    }
    
    public void Start()
    {
        _chooseSaveScreen.Choose += Choose;
    }

    private void Choose()
    {
        if (_isTransitioning || !_saveListUI.HasSelection)
            return;

        if (_saveListUI.IsNewGameSelected)
        {
            _afterSelectScreen.ClearSave();
        }
        else
        {
            var save = _saveListUI.SelectedSave;
            _afterSelectScreen.SetSave(save.Name);
        }
        ChooseAsync().Forget();
    }

    private async UniTask ChooseAsync()
    {
        _isTransitioning = true;
        _chooseSaveScreen.SetInteractable(false);
        try
        {
            await _chooseSaveScreen.Hide();
            await _afterSelectScreen.Show();
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public void Dispose()
    {
        _chooseSaveScreen.Choose -= Choose;
    }
}
