using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public class SettingsTabsHandler : IStartable, IDisposable
{
    private readonly SettingsScreen _screen;

    public SettingsTabsHandler(SettingsScreen screen)
    {
        _screen = screen;
    }

    public void Start()
    {
        _screen.General += OnGeneral;
        _screen.Controls += OnControls;
        _screen.Graphics += OnGraphics;
        _screen.Audio += OnAudio;
        _screen.Online += OnOnline;
        _screen.SelectGeneral(instant: true);
    }

    public void Dispose()
    {
        _screen.General -= OnGeneral;
        _screen.Controls -= OnControls;
        _screen.Graphics -= OnGraphics;
        _screen.Audio -= OnAudio;
        _screen.Online -= OnOnline;
    }

    private void OnGeneral(bool instant) => _screen.ChooseGeneral(instant).Forget();
    private void OnControls() => _screen.ChooseControls().Forget();
    private void OnGraphics() => _screen.ChooseGraphics().Forget();
    private void OnAudio() => _screen.ChooseAudio().Forget();
    private void OnOnline() => _screen.ChooseOnline().Forget();
}
