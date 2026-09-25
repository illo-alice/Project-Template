using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class AfterSelectScreenBackHandler : IStartable, IDisposable
{
    private readonly AfterSelectScreen _afterSelectScreen;
    private readonly ChooseSaveScreen _chooseSaveScreen;
    private bool _isTransitioning;

    public AfterSelectScreenBackHandler(AfterSelectScreen afterSelectScreen, ChooseSaveScreen chooseSaveScreen)
    {
        _afterSelectScreen = afterSelectScreen;
        _chooseSaveScreen = chooseSaveScreen;
    }

    public void Start() => _afterSelectScreen.Back += OnBack;

    private void OnBack()
    {
        if (!_isTransitioning)
            BackAsync().Forget();
    }

    private async UniTask BackAsync()
    {
        _isTransitioning = true;
        _afterSelectScreen.SetInteractable(false);
        try
        {
            await _afterSelectScreen.Hide();
            await _chooseSaveScreen.Show();
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public void Dispose() => _afterSelectScreen.Back -= OnBack;
}
