public sealed class ShowPingParameter : BoolParameter
{
    public override string CategoryKey => ParameterCategory.ONLINE;
    public override string SaveKey => $"{CategoryKey}.ShowPing";
    protected override void ApplyValue(bool value) { /* PingDisplay observes Changed. */ }
}
