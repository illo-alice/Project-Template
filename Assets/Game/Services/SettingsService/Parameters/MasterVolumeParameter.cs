public class MasterVolumeParameter : VolumeParameter
{
    public MasterVolumeParameter(AudioService audioService) : base(audioService)
    {
    }

    public override string SaveKey => $"{CategoryKey}.MasterVolume";

    protected override void ApplyVolume(float value) => AudioService.SetMasterVolume(value);
}
