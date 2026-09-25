public sealed class InvertCameraYParameter : BoolParameter
{
    public override string CategoryKey => ParameterCategory.CONTROLS;
    public override string SaveKey => $"{CategoryKey}.InvertCameraY";
    protected override void ApplyValue(bool value)
    {
        // Connect to the camera controller when it is implemented.
    }
}
