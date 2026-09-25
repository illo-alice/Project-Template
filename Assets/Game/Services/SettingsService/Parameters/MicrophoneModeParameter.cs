using System;
using System.Collections.Generic;

public sealed class MicrophoneModeParameter : ChoiceParameter
{
    private readonly VoiceChatService _voice;
    public MicrophoneModeParameter(VoiceChatService voice) => _voice = voice;
    public override string CategoryKey => ParameterCategory.AUDIO;
    public override string SaveKey => $"{CategoryKey}.MicrophoneMode";
    protected override string DefaultValue => "push_to_talk";
    protected override IEnumerable<SettingsChoice> CreateOptions()
    {
        yield return new SettingsChoice("off", SettingsText.Get("mic_off", "Off"));
        yield return new SettingsChoice("push_to_talk", SettingsText.Get("mic_ptt", "Push to talk"));
        yield return new SettingsChoice("voice_activation", SettingsText.Get("mic_vad", "Voice activation"));
    }
    protected override void ApplyValue(string id)
    {
        // Keep save IDs stable independently of enum names and option order.
        var mode = id switch
        {
            "off" => VoiceChatService.Mode.Off,
            "push_to_talk" => VoiceChatService.Mode.PushToTalk,
            "voice_activation" => VoiceChatService.Mode.VoiceActivation,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown microphone mode ID.")
        };
        _voice.SetMode(mode);
    }
}
