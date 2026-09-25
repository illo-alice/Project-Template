using VContainer;
using VContainer.Unity;

public class MenuLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<MenuFlow>(Lifetime.Scoped);
        builder.RegisterComponentInHierarchy<MainMenuScreen>();
        builder.RegisterComponentInHierarchy<JoinScreen>();
        builder.RegisterComponentInHierarchy<HostGameScreen>();
        builder.RegisterComponentInHierarchy<ChooseSaveScreen>();
        builder.RegisterComponentInHierarchy<AfterSelectScreen>();
        builder.RegisterComponentInHierarchy<SaveListUI>();
        builder.RegisterComponentInHierarchy<CreditsScreen>();
        builder.RegisterEntryPoint<MainMenuHideHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<MainMenuHostHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<MainMenuJoinGameHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<MainMenuCreditsHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<MainMenuSettingsHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<MainMenuQuitHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<ChooseSaveScreenBackHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<ChooseSaveScreenChooseHandler>();
        builder.RegisterEntryPoint<ChooseSaveScreenDeleteHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<AfterSelectScreenBackHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<AfterSelectScreenPlayHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<JoinGameJoinHandler>(Lifetime.Scoped);
        builder.RegisterEntryPoint<JoinScreenBackHandler>(Lifetime.Scoped);
        builder.InjectSceneUISounds(gameObject.scene);
    }
}
