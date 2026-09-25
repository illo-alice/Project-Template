public class SfxVolumeParameter : VolumeParameter
{
    public SfxVolumeParameter(AudioService audioService) : base(audioService)
    {
    }

    public override string SaveKey => $"{CategoryKey}.SFXVolume";

    protected override void ApplyVolume(float value) => AudioService.SetSfxVolume(value);
}
