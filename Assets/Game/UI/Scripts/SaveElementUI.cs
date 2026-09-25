using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public sealed class SaveElementUI : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _saved;
    [SerializeField] private TMP_Text _created;
    [SerializeField] private Outline _outline;

    private string _saveId;
    private Action<string> _onSelected;

    private void Awake() => _button.onClick.AddListener(Select);

    public void Bind(SaveInfo info, Action<string> onSelected)
    {
        _saveId = info.Id;
        _onSelected = onSelected;
        var culture = LocalizationSettings.SelectedLocale?.Identifier.CultureInfo ?? CultureInfo.CurrentCulture;
        _name.text = info.Name;
        _saved.text = string.Format(SettingsText.Get("save_last_saved_format", "Last saved: {0}"),
            info.SavedAtUtc.ToLocalTime().ToString("g", culture));
        _created.text = string.Format(SettingsText.Get("save_created_format", "Created: {0}"),
            info.CreatedAtUtc.ToLocalTime().ToString("g", culture));
    }

    public void BindNewGame(Action<string> onSelected)
    {
        _saveId = null;
        _onSelected = onSelected;
    }

    public void SetSelected(bool selected) => _outline.enabled = selected;
    private void Select() => _onSelected?.Invoke(_saveId);
    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(Select);
    }
}
