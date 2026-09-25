using UnityEngine.SceneManagement;
using VContainer;

public static class UISoundInjection
{
    /// <summary>Injects existing UI sounds, including those on hidden screens.</summary>
    public static void InjectSceneUISounds(this IContainerBuilder builder, Scene scene)
    {
        builder.RegisterBuildCallback(resolver =>
        {
            if (!scene.IsValid()) return;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var sound in root.GetComponentsInChildren<UISound>(includeInactive: true))
                resolver.Inject(sound);
        });
    }
}
