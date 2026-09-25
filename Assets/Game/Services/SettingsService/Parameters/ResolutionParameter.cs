using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public sealed class ResolutionParameter : ChoiceParameter
{
    private readonly DisplaySettingsService _display;
    public ResolutionParameter(DisplaySettingsService display) => _display = display;
    public override string CategoryKey => ParameterCategory.GRAPHICS;
    public override string SaveKey => $"{CategoryKey}.Resolution";
    protected override string DefaultValue => $"{Screen.width}x{Screen.height}";
    protected override IEnumerable<SettingsChoice> CreateOptions()
    {
        var sizes = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).ToList();
        sizes.Add(new Vector2Int(Screen.width, Screen.height));
        foreach (var size in sizes.Distinct().OrderByDescending(s => s.x).ThenByDescending(s => s.y))
            yield return new SettingsChoice($"{size.x}x{size.y}", $"{size.x} × {size.y}");
    }
    protected override void ApplyValue(string id)
    {
        var size = id.Split('x');
        _display.SetResolution(int.Parse(size[0], CultureInfo.InvariantCulture), int.Parse(size[1], CultureInfo.InvariantCulture));
    }
}
