using System.Collections.Generic;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

public class AppLifetimeScope : LifetimeScope
{
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] private Speaker _voiceSpeakerPrefab;
    
    protected override void Configure(IContainerBuilder builder)
    {
        // App services belong to Bootstrap. Scoped entry points would be
        // instantiated and started again by each child scene's LifetimeScope.
        builder.RegisterEntryPoint<AppFlow>(Lifetime.Singleton);
        builder.Register<Session>(Lifetime.Singleton);
        builder.Register<GameSaveService>(Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<SessionNetworkProxy>();
        builder.Register<DevelopAuthenticationProvider>(Lifetime.Singleton).As<IAuthenticationProvider>();
        builder.RegisterEntryPoint<SceneLoadProgressHandler>(Lifetime.Singleton);
        builder.Register<MasterVolumeParameter>(Lifetime.Singleton);
        builder.Register<MusicVolumeParameter>(Lifetime.Singleton);
        builder.Register<SfxVolumeParameter>(Lifetime.Singleton);
        builder.Register<VoiceChatVolumeParameter>(Lifetime.Singleton);
        builder.Register<UIVolumeParameter>(Lifetime.Singleton);
        builder.Register<AmbienceVolumeParameter>(Lifetime.Singleton);
        builder.Register<LanguageParameter>(Lifetime.Singleton);
        builder.RegisterInstance(_inputActions);
        builder.Register<GameInputService>(Lifetime.Singleton);
        builder.RegisterInstance(_voiceSpeakerPrefab);
        builder.RegisterEntryPoint<VoiceChatService>(Lifetime.Singleton).AsSelf();
        builder.RegisterComponentInHierarchy<PingDisplay>();
        builder.Register<MouseSensitivityParameter>(Lifetime.Singleton);
        builder.Register<InvertCameraYParameter>(Lifetime.Singleton);
        builder.Register<KeyBindingsParameter>(Lifetime.Singleton);
        builder.Register<DisplaySettingsService>(Lifetime.Singleton);
        builder.Register<ScreenModeParameter>(Lifetime.Singleton);
        builder.Register<ResolutionParameter>(Lifetime.Singleton);
        builder.Register<VSyncParameter>(Lifetime.Singleton);
        builder.Register<FrameRateLimitParameter>(Lifetime.Singleton);
        builder.Register<GraphicsQualityParameter>(Lifetime.Singleton);
        builder.Register<MicrophoneModeParameter>(Lifetime.Singleton);
        builder.Register<MicrophoneDeviceParameter>(Lifetime.Singleton);
        builder.Register<RegionParameter>(Lifetime.Singleton);
        builder.Register<ShowPingParameter>(Lifetime.Singleton);
        builder.Register(resolver =>
            new List<ISettingsParameter>
            {
                resolver.Resolve<MasterVolumeParameter>(),
                resolver.Resolve<MusicVolumeParameter>(),
                resolver.Resolve<SfxVolumeParameter>(),
                resolver.Resolve<VoiceChatVolumeParameter>(),
                resolver.Resolve<UIVolumeParameter>(),
                resolver.Resolve<AmbienceVolumeParameter>(),
                resolver.Resolve<LanguageParameter>(),
                resolver.Resolve<MouseSensitivityParameter>(),
                resolver.Resolve<InvertCameraYParameter>(),
                resolver.Resolve<KeyBindingsParameter>(),
                resolver.Resolve<ScreenModeParameter>(),
                resolver.Resolve<ResolutionParameter>(),
                resolver.Resolve<VSyncParameter>(),
                resolver.Resolve<FrameRateLimitParameter>(),
                resolver.Resolve<GraphicsQualityParameter>(),
                resolver.Resolve<MicrophoneModeParameter>(),
                resolver.Resolve<MicrophoneDeviceParameter>(),
                resolver.Resolve<RegionParameter>(),
                resolver.Resolve<ShowPingParameter>()
            }, Lifetime.Singleton);
        builder.Register<SettingsService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<AudioService>(Lifetime.Singleton).AsSelf().WithParameter<Transform>(transform);
        builder.RegisterInstance(_audioMixer);
        builder.RegisterComponentInHierarchy<LoadingScreen>();
        builder.RegisterComponentInHierarchy<MessageBox>();
        builder.RegisterComponentInHierarchy<PauseMenuToggle>();
        builder.RegisterComponentInHierarchy<PauseMenuScreen>();
        builder.RegisterEntryPoint<PauseMenuContinueHandler>(Lifetime.Singleton);
        builder.RegisterEntryPoint<PauseMenuLeaveHandler>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<PauseMenuDisconnectHandler>(Lifetime.Singleton);
        builder.RegisterEntryPoint<PauseMenuInviteHandler>(Lifetime.Singleton);
        builder.RegisterEntryPoint<PauseMenuSettingsHandler>(Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<SettingsScreen>();
        builder.RegisterEntryPoint<SettingsTabsHandler>(Lifetime.Singleton);
        builder.RegisterEntryPoint<SettingsBackHandler>(Lifetime.Singleton);
        builder.Register<RoomCodeService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<SteamService>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<SteamJoinRequestedHandler>(Lifetime.Singleton);
        builder.RegisterEntryPoint<PauseMenuRoomCodeHandler>(Lifetime.Singleton);
        builder.RegisterEntryPoint<PlayerSpawnService>(Lifetime.Singleton).AsSelf();
        builder.InjectSceneUISounds(gameObject.scene);
    }
}
