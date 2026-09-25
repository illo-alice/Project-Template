using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class SettingsBackHandler : IStartable, IDisposable
{
    private readonly SettingsScreen _screen;
    private readonly SettingsService _settingsService;
    
    private bool _isClosing;

    public SettingsBackHandler(SettingsScreen screen, SettingsService settingsService)
    {
        _screen = screen;
        _settingsService = settingsService;
    }

    public void Start() => _screen.Back += OnBack;

    public void Dispose() => _screen.Back -= OnBack;

    private void OnBack() => CloseAsync().Forget();

    private async UniTask CloseAsync()
    {
        if (_isClosing)
            return;

        _isClosing = true;
        _screen.SetInteractable(false);
        try
        {
            await _settingsService.SaveChanges();
            await _screen.Hide();
            _screen.SelectGeneral(instant: true);
        }
        catch
        {
            if (_screen != null)
                _screen.SetInteractable(true);
            throw;
        }
        finally
        {
            _isClosing = false;
        }
    }
}
