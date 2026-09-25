using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class MicrophoneDeviceParameter : ChoiceParameter
{
    private readonly VoiceChatService _voice;
    public MicrophoneDeviceParameter(VoiceChatService voice) => _voice = voice;
    public override string CategoryKey => ParameterCategory.AUDIO;
    public override string SaveKey => $"{CategoryKey}.MicrophoneDevice";
    protected override string DefaultValue => "default";
    protected override IEnumerable<SettingsChoice> CreateOptions()
    {
        yield return new SettingsChoice("default", SettingsText.Get("system_default", "System default"));
        foreach (var device in Microphone.devices.Distinct()) yield return new SettingsChoice("device:" + device, device);
    }
    protected override void ApplyValue(string id) => _voice.SetDevice(id == "default" ? null : id.Substring("device:".Length));
    public void RefreshDevices()
    {
        RefreshOptions();
        if (Value != null && !Contains(Value)) SetValue(DefaultValue);
    }
}
