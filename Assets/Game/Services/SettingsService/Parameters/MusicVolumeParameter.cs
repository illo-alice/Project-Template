public class MusicVolumeParameter : VolumeParameter
{
    public MusicVolumeParameter(AudioService audioService) : base(audioService)
    {
    }

    public override string SaveKey => $"{CategoryKey}.MusicVolume";

    protected override void ApplyVolume(float value) => AudioService.SetMusicVolume(value);
}
