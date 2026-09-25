/// <summary>A single playback. A stopped handle cannot affect a reused source.</summary>
public readonly struct AudioHandle
{
    private readonly AudioService _service;
    private readonly ulong _id;

    internal AudioHandle(AudioService service, ulong id)
    {
        _service = service;
        _id = id;
    }

    public bool IsPlaying => _service != null && _service.IsPlaying(_id);

    public void Stop(float fadeOut = 0f) => _service?.Stop(_id, fadeOut);
}
