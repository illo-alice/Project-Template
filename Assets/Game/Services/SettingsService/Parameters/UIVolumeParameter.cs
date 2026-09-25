public class UIVolumeParameter : VolumeParameter
{
    public UIVolumeParameter(AudioService audioService) : base(audioService)
    {
    }

    public override string SaveKey => $"{CategoryKey}.UIVolume";

    protected override void ApplyVolume(float value) => AudioService.SetUIVolume(value);
}
