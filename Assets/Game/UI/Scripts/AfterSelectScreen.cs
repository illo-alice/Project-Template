using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public sealed class AfterSelectScreen : MonoBehaviour
{
    [SerializeField] private CanvasFader _fader;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private SettingsSliderElement _maxPlayersSlider;
    [SerializeField] private TMP_Dropdown _lobbyAccessDropdown;
    [SerializeField] private Button _play;
    [SerializeField] private Button _back;

    public event Action Play;
    public event Action Back;
    
    public string SaveName => string.IsNullOrEmpty(_nameInput.text) ? "New Game" : _nameInput.text;
    public int MaxPlayers => Mathf.RoundToInt(_maxPlayersSlider.Value);
    public LobbyAccessMode AccessMode => (LobbyAccessMode)_lobbyAccessDropdown.value;

    private void Awake()
    {
        _play.onClick.AddListener(OnPlay);
        _back.onClick.AddListener(OnBack);
        LocalizationSettings.SelectedLocaleChanged += RefreshAccessOptions;
        RefreshAccessOptions(null);
    }

    private void RefreshAccessOptions(Locale locale)
    {
        var selected = _lobbyAccessDropdown.value;
        _lobbyAccessDropdown.ClearOptions();
        _lobbyAccessDropdown.AddOptions(new List<string>
        {
            S.UI("lobby_access_invite_only"),
            S.UI("lobby_access_friends_only"),
            S.UI("lobby_access_closed")
        });
        _lobbyAccessDropdown.SetValueWithoutNotify(selected);
        _lobbyAccessDropdown.RefreshShownValue();
    }

    public void SetSave(string saveName)
    {
        _nameInput.SetTextWithoutNotify(saveName);
        _nameInput.interactable = false;
    }

    public void ClearSave()
    {
        _nameInput.SetTextWithoutNotify(string.Empty);
        _nameInput.interactable = true;
    }

    public UniTask Show(bool instant = false) => _fader.Show(instant);
    public UniTask Hide(bool instant = false) => _fader.Hide(instant);
    public void SetInteractable(bool interactable) => _fader.SetInteractable(interactable);

    private void OnPlay() => Play?.Invoke();
    private void OnBack() => Back?.Invoke();

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= RefreshAccessOptions;
        if (_play != null) _play.onClick.RemoveListener(OnPlay);
        if (_back != null) _back.onClick.RemoveListener(OnBack);
    }
}
