using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GraphicsQualityParameter : ChoiceParameter
{
    private readonly VSyncParameter _vSync;
    public GraphicsQualityParameter(VSyncParameter vSync) => _vSync = vSync;
    public override string CategoryKey => ParameterCategory.GRAPHICS;
    public override string SaveKey => $"{CategoryKey}.Quality";
    protected override string DefaultValue => QualitySettings.names[QualitySettings.GetQualityLevel()];
    protected override IEnumerable<SettingsChoice> CreateOptions()
    {
        foreach (var name in QualitySettings.names) yield return new SettingsChoice(name, name);
    }
    protected override void ApplyValue(string id)
    {
        var index = Array.IndexOf(QualitySettings.names, id);
        QualitySettings.SetQualityLevel(index, true);
        // Quality presets include vSyncCount; preserve the user's separate setting.
        QualitySettings.vSyncCount = _vSync.Value ? 1 : 0;
    }
}
