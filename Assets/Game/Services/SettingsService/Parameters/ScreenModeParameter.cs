using System.Collections.Generic;
using UnityEngine;

public sealed class ScreenModeParameter : ChoiceParameter
{
    private readonly DisplaySettingsService _display;
    public ScreenModeParameter(DisplaySettingsService display) => _display = display;
    public FullScreenMode SelectedMode => Value == "windowed" ? FullScreenMode.Windowed : Value == "exclusive" ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
    public override string CategoryKey => ParameterCategory.GRAPHICS;
    public override string SaveKey => $"{CategoryKey}.ScreenMode";
    protected override string DefaultValue => Screen.fullScreenMode == FullScreenMode.Windowed ? "windowed"
        : Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen ? "exclusive" : "borderless";
    protected override IEnumerable<SettingsChoice> CreateOptions()
    {
        yield return new SettingsChoice("windowed", SettingsText.Get("windowed", "Windowed"));
        yield return new SettingsChoice("borderless", SettingsText.Get("borderless", "Borderless"));
        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            yield return new SettingsChoice("exclusive", SettingsText.Get("fullscreen", "Fullscreen"));
    }
    protected override void ApplyValue(string id)
    {
        _display.SetMode(id == "windowed" ? FullScreenMode.Windowed
            : id == "exclusive" ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow);
    }
}
