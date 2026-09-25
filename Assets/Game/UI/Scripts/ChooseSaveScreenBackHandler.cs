using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class ChooseSaveScreenBackHandler : IStartable, IDisposable
{
    private readonly ChooseSaveScreen _chooseSaveScreen;
    private readonly HostGameScreen _hostScreen;
    private readonly MainMenuScreen _mainMenuScreen;
    private bool _isTransitioning;

    public ChooseSaveScreenBackHandler(
        ChooseSaveScreen chooseSaveScreen, HostGameScreen hostScreen, MainMenuScreen mainMenuScreen)
    {
        _chooseSaveScreen = chooseSaveScreen;
        _hostScreen = hostScreen;
        _mainMenuScreen = mainMenuScreen;
    }

    public void Start() => _chooseSaveScreen.Back += OnBack;

    private void OnBack()
    {
        if (!_isTransitioning)
            BackAsync().Forget();
    }

    private async UniTask BackAsync()
    {
        _isTransitioning = true;
        _chooseSaveScreen.SetInteractable(false);
        try
        {
            _mainMenuScreen.Show().Forget();
            await _chooseSaveScreen.Hide();
            await _hostScreen.Hide(instant: true);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public void Dispose() => _chooseSaveScreen.Back -= OnBack;
}
