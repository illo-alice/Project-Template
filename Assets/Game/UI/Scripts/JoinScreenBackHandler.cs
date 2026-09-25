using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class JoinScreenBackHandler : IStartable, IDisposable
{
    private readonly JoinScreen _joinScreen;
    private readonly MainMenuScreen _mainMenuScreen;
    private bool _isTransitioning;

    public JoinScreenBackHandler(JoinScreen joinScreen, MainMenuScreen mainMenuScreen)
    {
        _joinScreen = joinScreen;
        _mainMenuScreen = mainMenuScreen;
    }

    public void Start() => _joinScreen.Back += OnBack;

    private void OnBack()
    {
        if (!_isTransitioning)
            BackAsync().Forget();
    }

    private async UniTask BackAsync()
    {
        _isTransitioning = true;
        _joinScreen.SetInteractable(false);
        try
        {
            await _joinScreen.Hide();
            await _mainMenuScreen.Show();
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public void Dispose() => _joinScreen.Back -= OnBack;
}
