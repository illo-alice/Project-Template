using UnityEngine;

public sealed class VSyncParameter : BoolParameter
{
    public override string CategoryKey => ParameterCategory.GRAPHICS;
    public override string SaveKey => $"{CategoryKey}.VSync";
    protected override void ApplyValue(bool value) => QualitySettings.vSyncCount = value ? 1 : 0;
}
