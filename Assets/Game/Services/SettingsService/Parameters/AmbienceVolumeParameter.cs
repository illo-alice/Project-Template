public class AmbienceVolumeParameter : VolumeParameter
{
    public AmbienceVolumeParameter(AudioService audioService) : base(audioService)
    {
    }

    public override string SaveKey => $"{CategoryKey}.AmbienceVolume";

    protected override void ApplyVolume(float value) => AudioService.SetAmbienceVolume(value);
}
