using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using VContainer;
using VContainer.Unity;

public class AppFlow : IStartable, IDisposable
{
    private Session _session;
    private SteamService _steamService;
    private SettingsService _settingsService;
    private GameSaveService _gameSaveService;
    private LoadingScreen _loadingScreen;
    private MessageBox _messageBox;
    private CancellationTokenSource _flowCancellation;

#if !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void EnsureSteamLaunch()
    {
        try
        {
            if (!SteamClient.RestartAppIfNecessary(SteamService.AppId)) return;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        // Only during early startup: bypass runtime shutdown, which can hang
        // inside Unity. Terminate this process without touching the Steam relaunch.
        System.Diagnostics.Process.GetCurrentProcess().Kill();
    }
#endif
    
    [Inject]
    public void Construct(
        Session session,
        SteamService steamService,
        SettingsService settingsService,
        GameSaveService gameSaveService,
        LoadingScreen loadingScreen, 
        MessageBox messageBox)
    {
        _session = session;
        _steamService = steamService;
        _settingsService = settingsService;
        _gameSaveService =  gameSaveService;
        _loadingScreen = loadingScreen;
        _messageBox = messageBox;
    }
    
    public void Start()
    {
        _flowCancellation = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
        StartFlow(_flowCancellation.Token).Forget();
    }

    public void Dispose()
    {
        _flowCancellation?.Cancel();
        _flowCancellation?.Dispose();
        _flowCancellation = null;
    }

    private async UniTask StartFlow(CancellationToken cancellationToken)
    {
        try
        {
            await StartFlowCore(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Exit Play Mode or dispose Bootstrap without continuing startup.
        }
    }

    private async UniTask StartFlowCore(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            _steamService.Initialize();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            await LocalizationSettings.InitializationOperation.ToUniTask(cancellationToken: cancellationToken);
            await LocalizationSettings.StringDatabase.PreloadTables("UI").ToUniTask(cancellationToken: cancellationToken);
            await _messageBox.Show(S.UI("steam_init_failed"), S.UI("steam_init_failed_description"),
                S.UI("btn_quit"), cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        await _loadingScreen.SelectBar(LoadingScreen.BarType.LoadingBar, cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var strings = LocalizationSettings.StringDatabase;
        var statusText = await strings.GetLocalizedStringAsync("Boot", "localization_loading")
            .ToUniTask(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        _loadingScreen.SetStatus(statusText);
        await _loadingScreen.Show(instant: true, cancellationToken: cancellationToken);
        
        await LocalizationSettings.InitializationOperation.ToUniTask(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await strings.PreloadTables("UI").ToUniTask(cancellationToken: cancellationToken);

        _loadingScreen.SetStatus(strings.GetLocalizedString("UI", "applying_settings"));
        try
        {
            var status = await _settingsService.InitializeAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            switch (status)
            {
                case SettingsLoadStatus.Loaded:
                case SettingsLoadStatus.CreatedDefaults:
                    break;

                case SettingsLoadStatus.RestoredBackup:
                    await _messageBox.Show(
                        strings.GetLocalizedString("UI", "settings_recovery_title"),
                        strings.GetLocalizedString("UI", "settings_restore_backup"),
                        strings.GetLocalizedString("UI", "btn_understood"),
                        cancellationToken: cancellationToken);
                    break;

                case SettingsLoadStatus.ResetToDefaults:
                    await _messageBox.Show(
                        strings.GetLocalizedString("UI", "settings_recovery_title"),
                        strings.GetLocalizedString("UI", "settings_restore_defaults"),
                        strings.GetLocalizedString("UI", "btn_understood"),
                        cancellationToken: cancellationToken);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.LogException(exception);
            await _messageBox.Show(
                strings.GetLocalizedString("UI", "settings_load_failed"),
                strings.GetLocalizedString("UI", "settings_load_failed_description"),
                strings.GetLocalizedString("UI", "btn_quit"),
                cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _loadingScreen.SetStatus(
                strings.GetLocalizedString("UI", "connecting"));
        
            var connectionResult = await _session.Connect(cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!connectionResult.Ok)
            {
                _loadingScreen.SetStatus(
                    strings.GetLocalizedString("UI", "disconnected"));
                var option = await _messageBox.Show(
                    strings.GetLocalizedString("UI", "connection_failed"),
                    strings.GetLocalizedString("UI", "connection_retry_prompt"),
                    strings.GetLocalizedString("UI", "try_again"),
                    strings.GetLocalizedString("UI", "play_offline"),
                    cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                
                if (option == MessageBox.Option.First)
                    continue;
            }
            
            break;
        }
        _loadingScreen.SetStatus(strings.GetLocalizedString("UI", "save_initializing"));
        await _gameSaveService.InitializeAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await _loadingScreen.SelectBar(LoadingScreen.BarType.ProgressBar, cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        _loadingScreen.SetStatus(strings.GetLocalizedString("UI", "loading"));
        await _session.LoadMenu(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await _loadingScreen.Hide(cancellationToken: cancellationToken);
        _steamService.EnableJoinRequests();
    }

}
