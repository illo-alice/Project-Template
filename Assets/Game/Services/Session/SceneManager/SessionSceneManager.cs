using System;
using Fusion;

public class SessionSceneManager : NetworkSceneManagerDefault
{
    public event Action<float> ProgressChanged;

    protected override void OnLoadSceneProgress(
        SceneRef sceneRef, float progress)
    {
        base.OnLoadSceneProgress(sceneRef, progress);
        ProgressChanged?.Invoke(progress);
    }
}