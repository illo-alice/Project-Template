using System;
using VContainer.Unity;

public sealed class SceneLoadProgressHandler : IInitializable, IDisposable
{
    private readonly Session _session;
    private readonly LoadingScreen _loadingScreen;

    public SceneLoadProgressHandler(Session session, LoadingScreen loadingScreen)
    {
        _session = session;
        _loadingScreen = loadingScreen;
    }

    public void Initialize()
    {
        _session.SceneLoadProgressChanged += HandleProgress;
    }

    public void Dispose()
    {
        _session.SceneLoadProgressChanged -= HandleProgress;
    }

    private void HandleProgress(float progress)
    {
        _loadingScreen.SetProgress(progress);
    }
}
