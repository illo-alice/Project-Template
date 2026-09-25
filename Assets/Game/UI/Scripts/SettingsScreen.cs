using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SettingsScreen : MonoBehaviour
{
    public event Action Hidden;
    public event Action<bool> General;
    public event Action Controls;
    public event Action Graphics;
    public event Action Audio;
    public event Action Online;
    public event Action Back;

    [SerializeField] private CanvasFader _fader;

    [SerializeField] private Toggle _general;
    [SerializeField] private Toggle _controls;
    [SerializeField] private Toggle _graphics;
    [SerializeField] private Toggle _audio;
    [SerializeField] private Toggle _online;
    [SerializeField] private Button _back;

    [SerializeField] private CanvasFader _generalFader;
    [SerializeField] private CanvasFader _controlsFader;
    [SerializeField] private CanvasFader _graphicsFader;
    [SerializeField] private CanvasFader _audioFader;
    [SerializeField] private CanvasFader _onlineFader;
    
    [SerializeField] private SettingsSliderElement _masterVolumeSlider;
    [SerializeField] private SettingsSliderElement _musicVolumeSlider;
    [SerializeField] private SettingsSliderElement _sfxVolumeSlider;
    [SerializeField] private SettingsSliderElement _voiceChatVolumeSlider;
    [SerializeField] private SettingsSliderElement _uiVolumeSlider;
    [SerializeField] private SettingsSliderElement _ambienceVolumeSlider;
    
    [SerializeField] private SettingsDropDownElement _languageDropDown;
    
    [SerializeField] private SettingsSliderElement _mouseSensitivitySlider;
    [SerializeField] private SettingsToggleElement _invertYToggle;
    [SerializeField] private SettingsKeyBindingsList _keyBindingsList;
    [SerializeField] private SettingsDropDownElement _screenModeDropDown;
    [SerializeField] private SettingsDropDownElement _resolutionDropDown;
    [SerializeField] private SettingsToggleElement _vSyncToggle;
    [SerializeField] private SettingsSliderElement _frameRateSlider;
    [SerializeField] private SettingsDropDownElement _qualityDropDown;
    [SerializeField] private SettingsDropDownElement _microphoneModeDropDown;
    [SerializeField] private SettingsDropDownElement _microphoneDeviceDropDown;
    [SerializeField] private SettingsDropDownElement _regionDropDown;
    [SerializeField] private SettingsToggleElement _showPingToggle;
    private ChoiceParameter[] _choices;
    private MicrophoneDeviceParameter _microphoneDevice;
    private RegionParameter _region;
    private KeyBindingsParameter _keyBindings;

    private CanvasFader _currentFader;
    private CancellationTokenSource _tabCancellation;
    private CancellationToken _destroyToken;
    private bool _instantSelection;

    private void Awake()
    {
        _destroyToken = destroyCancellationToken;
        _general.onValueChanged.AddListener(OnGeneralChanged);
        _controls.onValueChanged.AddListener(OnControlsChanged);
        _graphics.onValueChanged.AddListener(OnGraphicsChanged);
        _audio.onValueChanged.AddListener(OnAudioChanged);
        _online.onValueChanged.AddListener(OnOnlineChanged);
        _back.onClick.AddListener(OnBackClicked);
    }
    
    [Inject]
    public void Construct(
        MasterVolumeParameter masterVolume,
        MusicVolumeParameter musicVolume,
        SfxVolumeParameter sfxVolume,
        VoiceChatVolumeParameter voiceChatVolume,
        UIVolumeParameter uiVolume,
        AmbienceVolumeParameter ambienceVolume,
        LanguageParameter language,
        MouseSensitivityParameter mouseSensitivity, InvertCameraYParameter invertY, KeyBindingsParameter keyBindings,
        ScreenModeParameter screenMode, ResolutionParameter resolution, VSyncParameter vSync,
        FrameRateLimitParameter frameRate, GraphicsQualityParameter quality,
        MicrophoneModeParameter microphoneMode, MicrophoneDeviceParameter microphoneDevice,
        RegionParameter region, ShowPingParameter showPing, IObjectResolver resolver)
    {
        _masterVolumeSlider.Bind(masterVolume);
        _musicVolumeSlider.Bind(musicVolume);
        _sfxVolumeSlider.Bind(sfxVolume);
        _voiceChatVolumeSlider.Bind(voiceChatVolume);
        _uiVolumeSlider.Bind(uiVolume);
        _ambienceVolumeSlider.Bind(ambienceVolume);
        _languageDropDown.Bind(language);
        _mouseSensitivitySlider.Bind(mouseSensitivity);
        _invertYToggle.Bind(invertY);
        _keyBindingsList.Bind(keyBindings, resolver);
        _screenModeDropDown.Bind(screenMode);
        _resolutionDropDown.Bind(resolution);
        _vSyncToggle.Bind(vSync);
        _frameRateSlider.Bind(frameRate);
        _qualityDropDown.Bind(quality);
        _microphoneModeDropDown.Bind(microphoneMode);
        _microphoneDeviceDropDown.Bind(microphoneDevice);
        _regionDropDown.Bind(region);
        _showPingToggle.Bind(showPing);
        _choices = new ChoiceParameter[] { screenMode, resolution, quality, microphoneMode, microphoneDevice, region };
        _microphoneDevice = microphoneDevice;
        _region = region;
        _keyBindings = keyBindings;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    public async UniTask Show(bool instant = false)
    {
        RefreshOptions();
        _region.RefreshRegionsAsync(_destroyToken).Forget();
        if (_currentFader == null)
            await ChooseTab(_generalFader, true);

        await _fader.Show(instant);
    }

    public async UniTask Hide(bool instant = false)
    {
        _keyBindings?.CancelRebind();
        await _fader.Hide(instant);
        Hidden?.Invoke();
    }

    public void SetInteractable(bool interactable) => _fader.SetInteractable(interactable);

    public void SelectGeneral(bool instant = false)
    {
        if (_general.isOn)
        {
            General?.Invoke(instant);
            return;
        }

        _instantSelection = instant;
        try
        {
            _general.isOn = true;
        }
        finally
        {
            _instantSelection = false;
        }
    }

    public UniTask ChooseGeneral(bool instant = false) => ChooseTab(_generalFader, instant);
    public UniTask ChooseControls() => ChooseTab(_controlsFader);
    public UniTask ChooseGraphics() => ChooseTab(_graphicsFader);
    public UniTask ChooseAudio() => ChooseTab(_audioFader);
    public UniTask ChooseOnline() => ChooseTab(_onlineFader);

    private void OnLocaleChanged(Locale locale) => RefreshOptions();
    private void RefreshOptions()
    {
        if (_choices == null) return;
        foreach (var choice in _choices) choice.RefreshOptions();
        _microphoneDevice.RefreshDevices();
        _keyBindingsList.Rebuild();
    }

    private void OnGeneralChanged(bool selected)
    {
        if (selected) General?.Invoke(_instantSelection);
    }

    private void OnControlsChanged(bool selected)
    {
        if (selected) Controls?.Invoke();
    }

    private void OnGraphicsChanged(bool selected)
    {
        if (selected) Graphics?.Invoke();
    }

    private void OnAudioChanged(bool selected)
    {
        if (selected) Audio?.Invoke();
    }

    private void OnOnlineChanged(bool selected)
    {
        if (selected) Online?.Invoke();
    }

    private void OnBackClicked() => Back?.Invoke();

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        if (_general != null) _general.onValueChanged.RemoveListener(OnGeneralChanged);
        if (_controls != null) _controls.onValueChanged.RemoveListener(OnControlsChanged);
        if (_graphics != null) _graphics.onValueChanged.RemoveListener(OnGraphicsChanged);
        if (_audio != null) _audio.onValueChanged.RemoveListener(OnAudioChanged);
        if (_online != null) _online.onValueChanged.RemoveListener(OnOnlineChanged);
        if (_back != null) _back.onClick.RemoveListener(OnBackClicked);
    }

    private async UniTask ChooseTab(CanvasFader nextFader, bool instant = false)
    {
        _keyBindings?.CancelRebind();
        if (this == null)
            throw new OperationCanceledException();

        if (!_destroyToken.CanBeCanceled)
            _destroyToken = destroyCancellationToken;
        _destroyToken.ThrowIfCancellationRequested();
        if (_currentFader == nextFader && _tabCancellation == null)
            return;

        _tabCancellation?.Cancel();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_destroyToken);
        _tabCancellation = cancellation;

        try
        {
            if (_currentFader != null)
                await _currentFader.Hide(instant, cancellation.Token);

            cancellation.Token.ThrowIfCancellationRequested();
            _currentFader = nextFader;
            await _currentFader.Show(instant, cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // A newer selection or destruction of the screen cancels this transition.
        }
        finally
        {
            if (_tabCancellation == cancellation)
                _tabCancellation = null;

            cancellation.Dispose();
        }
    }
}
