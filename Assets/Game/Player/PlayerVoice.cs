using System;
using Fusion;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using VContainer;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class PlayerVoice : VoiceNetworkObject
{
    [SerializeField] private Recorder _recorder;

    private VoiceChatService _service;

    [Inject]
    public void Construct(VoiceChatService service)
    {
        _service = service;
    }

    public override void Spawned()
    {
        if (_service == null)
            throw new InvalidOperationException("PlayerVoice must be instantiated through SessionObjectProvider.");
        if (_recorder == null)
            throw new InvalidOperationException("PlayerVoice recorder is not assigned.");

        // Configure ownership before Photon registers the local recorder.
        _service.BindRecorder(_recorder, IsLocal);
        try
        {
            base.Spawned();
        }
        catch
        {
            _service.UnbindRecorder(_recorder);
            throw;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _service.UnbindRecorder(_recorder);
        base.Despawned(runner, hasState);
    }
}
