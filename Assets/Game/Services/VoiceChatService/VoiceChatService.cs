using System.Linq;
using System;
using Cysharp.Threading.Tasks;
using Fusion;
using Photon.Voice;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

public sealed class VoiceChatService : ITickable, IDisposable
{
    public enum Mode
    {
        Off,
        PushToTalk,
        VoiceActivation
    }

    private readonly Speaker _speakerPrefab;
    private readonly KeyBindingsParameter _bindings;
    private Recorder _recorder;
    private NetworkRunner _runner;
    private FusionVoiceClient _voice;
    private readonly InputAction _pushToTalk;
    private Mode _mode = Mode.PushToTalk;
    private string _device;
    private float _nextDeviceCheck;
    private bool _hasMicrophone;
    private bool _stopping;

    public VoiceChatService(GameInputService input, KeyBindingsParameter bindings, Speaker speakerPrefab)
    {
        _bindings = bindings;
        _pushToTalk = input.Actions.FindAction("Voice/PushToTalk", true);
        _speakerPrefab = speakerPrefab;
    }

    public void BindRecorder(Recorder recorder, bool isLocal)
    {
        if (recorder == null)
            throw new ArgumentNullException(nameof(recorder));

        recorder.RecordWhenJoined = false;
        recorder.TransmitEnabled = false;
        recorder.RecordingEnabled = false;
        recorder.enabled = isLocal;

        if (!isLocal)
            return;

        StopRecording();
        _recorder = recorder;

        _recorder.MicrophoneType = Recorder.MicType.Unity;
        _recorder.UseMicrophoneTypeFallback = false;
        _recorder.StopRecordingWhenPaused = true;
        _recorder.VoiceDetectionThreshold = 0.01f;
        _recorder.VoiceDetectionDelayMs = 500;
        _recorder.SamplingRate = POpusCodec.Enums.SamplingRate.Sampling24000;
        _recorder.Bitrate = 30000;

        RefreshDevice();
    }

    public void UnbindRecorder(Recorder recorder)
    {
        if (_recorder != recorder)
            return;

        StopRecording();
        _recorder = null;
    }
    
    public void Attach(NetworkRunner runner)
    {
        if (runner == null)
            throw new ArgumentNullException(nameof(runner));

        if (_runner != null)
            throw new InvalidOperationException(
                "Voice is already attached to a session.");

        _stopping = false;
        _runner = runner;

        _voice = runner.gameObject.AddComponent<FusionVoiceClient>();
        _voice.ApplyDontDestroyOnLoad = false;

        // Запасной Speaker, если на персонаже нет своего.
        if (_speakerPrefab != null)
            _voice.SpeakerPrefab = _speakerPrefab.gameObject;

        runner.AddCallbacks(_voice);
    }

    public async UniTask StopAsync()
    {
        _stopping = true;
        StopRecording();
        if (_voice == null)
        {
            Detach();
            return;
        }
        _voice.AutoConnectAndJoin = false;
        try
        {
            // Keep the Voice client alive until it has serviced its disconnect.
            await Photon.Realtime.AsyncExtensions.DisconnectAsync(_voice.Client)
                .AsUniTask().Timeout(TimeSpan.FromSeconds(5));
        }
        catch (Exception exception)
        {
            if (!Application.exitCancellationToken.IsCancellationRequested)
                Debug.LogWarning($"Voice disconnect did not finish cleanly: {exception.Message}");
        }
        finally
        {
            Detach();
        }
    }

    public void Detach()
    {
        _stopping = true;
        StopRecording();
        if (_voice != null)
        {
            _voice.AutoConnectAndJoin = false;
            if (_runner != null) _runner.RemoveCallbacks(_voice);
        }
        // Components are destroyed with the runner by Session.
        _recorder = null;
        _voice = null;
        _runner = null;
    }

    public void SetMode(Mode mode)
    {
        if (mode != Mode.Off && mode != Mode.PushToTalk && mode != Mode.VoiceActivation)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown microphone mode.");

        _mode = mode;
        if (_recorder != null)
        {
            _recorder.TransmitEnabled = false;
            _recorder.VoiceDetection = mode == Mode.VoiceActivation;
            if (mode == Mode.Off) _recorder.RecordingEnabled = false;
        }
    }
    public void SetDevice(string device) { _device = device; ApplyDevice(); }
    private void ApplyDevice()
    {
        if (_recorder == null) return;
        _recorder.MicrophoneDevice = string.IsNullOrEmpty(_device) || !Microphone.devices.Contains(_device)
            ? DeviceInfo.Default : new DeviceInfo(_device);
    }
    public void Tick()
    {
        if (_recorder == null) return;
        if (Time.unscaledTime >= _nextDeviceCheck) RefreshDevice();
        var active = !_stopping && _runner != null && _runner.IsRunning && _runner.GameMode != GameMode.Single &&
            _voice != null && _voice.Client.InRoom &&
            _mode != Mode.Off && Application.isFocused && _hasMicrophone;
        _recorder.RecordingEnabled = active;
        _recorder.VoiceDetection = _mode == Mode.VoiceActivation;
        _recorder.TransmitEnabled = active && !_bindings.IsRebinding &&
            (_mode == Mode.VoiceActivation || (_pushToTalk != null && _pushToTalk.IsPressed()));
    }
    private void RefreshDevice()
    {
        _nextDeviceCheck = Time.unscaledTime + 2f;
        _hasMicrophone = Microphone.devices.Length > 0;
        ApplyDevice();
    }
    private void StopRecording()
    {
        if (_recorder == null) return;
        _recorder.TransmitEnabled = false;
        _recorder.RecordingEnabled = false;
    }

    public void Dispose() => Detach();
}
