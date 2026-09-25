using VContainer;
using VContainer.Unity;

public class LobbyLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<ScenePlayerSpawnProvider>();
        builder.RegisterEntryPoint<LobbyFlow>(Lifetime.Singleton);
        builder.InjectSceneUISounds(gameObject.scene);
    }
}
