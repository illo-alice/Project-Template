public class VoiceChatVolumeParameter : VolumeParameter
{
    public VoiceChatVolumeParameter(AudioService audioService) : base(audioService)
    {
    }

    public override string SaveKey => $"{CategoryKey}.VoiceChatVolume";

    protected override void ApplyVolume(float value) => AudioService.SetVoiceChatVolume(value);
}
