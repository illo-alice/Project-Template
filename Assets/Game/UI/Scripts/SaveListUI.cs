using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public sealed class SaveListUI : MonoBehaviour
{
    [SerializeField] private SaveElementUI _elementPrefab;
    [SerializeField] private SaveElementUI _newGamePrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private Image _preview;
    [SerializeField] private TMP_Text _emptyLabel;
    [SerializeField] private TMP_Text _previewPlaceholder;
    [SerializeField] private Button _playButton;

    private readonly Dictionary<string, SaveElementUI> _elements = new();
    private SaveElementUI _newGameElement;
    private GameSaveService _saveService;
    private IObjectResolver _resolver;
    private Texture2D _previewTexture;
    private Sprite _previewSprite;
    private byte[] _previewData;

    public SaveInfo SelectedSave { get; private set; }
    public bool IsNewGameSelected { get; private set; }
    public bool HasSelection => IsNewGameSelected || SelectedSave != null;
    public event Action<SaveInfo> SelectionChanged;

    [Inject]
    public void Construct(GameSaveService saveService, IObjectResolver resolver)
    {
        _saveService = saveService;
        _resolver = resolver;
        _saveService.SavesChanged += Refresh;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        Refresh();
    }

    private void OnEnable()
    {
        if (_saveService != null) Refresh();
    }

    public void Refresh()
    {
        var wasNewGameSelected = IsNewGameSelected;
        var selectedId = SelectedSave?.Id ?? _saveService.CurrentSave?.Id;
        if (_newGameElement != null)
        {
            _newGameElement.gameObject.SetActive(false);
            Destroy(_newGameElement.gameObject);
        }
        foreach (var element in _elements.Values)
        {
            element.gameObject.SetActive(false);
            Destroy(element.gameObject);
        }
        _elements.Clear();

        _newGameElement = _resolver.Instantiate(_newGamePrefab, _content);
        _newGameElement.BindNewGame(Select);
        _newGameElement.transform.SetAsFirstSibling();

        var saves = _saveService.GetSavesForUI();
        foreach (var info in saves)
        {
            var element = _resolver.Instantiate(_elementPrefab, _content);
            element.Bind(info, Select);
            _elements.Add(info.Id, element);
        }

        _emptyLabel.text = SettingsText.Get("saves_empty", "No saves yet");
        _emptyLabel.gameObject.SetActive(saves.Count == 0);
        _previewPlaceholder.text = SettingsText.Get("save_no_preview", "No preview");
        Select(wasNewGameSelected ? null :
            saves.FirstOrDefault(info => info.Id == selectedId)?.Id ?? saves.FirstOrDefault()?.Id);
    }

    public void Select(string saveId)
    {
        IsNewGameSelected = saveId == null;
        SelectedSave = IsNewGameSelected ? null :
            _saveService.GetSavesForUI().FirstOrDefault(info => info.Id == saveId);
        _newGameElement.SetSelected(IsNewGameSelected);
        foreach (var (id, element) in _elements)
            element.SetSelected(id == SelectedSave?.Id);

        ShowPreview(SelectedSave?.PreviewImage);
        _playButton.interactable = HasSelection;
        SelectionChanged?.Invoke(SelectedSave);
    }

    private void ShowPreview(byte[] png)
    {
        if (!ReferenceEquals(_previewData, png))
        {
            ReleasePreview();
            _previewData = png;
            if (png is { Length: > 0 })
            {
                _previewTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (_previewTexture.LoadImage(png, markNonReadable: true))
                    _previewSprite = Sprite.Create(_previewTexture,
                        new Rect(0, 0, _previewTexture.width, _previewTexture.height), new Vector2(0.5f, 0.5f));
                else
                {
                    Destroy(_previewTexture);
                    _previewTexture = null;
                }
            }
        }

        _preview.sprite = _previewSprite;
        _preview.enabled = _previewSprite != null;
        _previewPlaceholder.gameObject.SetActive(_previewSprite == null);
    }

    private void ReleasePreview()
    {
        if (_preview != null) _preview.sprite = null;
        if (_previewSprite != null) Destroy(_previewSprite);
        if (_previewTexture != null) Destroy(_previewTexture);
        _previewSprite = null;
        _previewTexture = null;
        _previewData = null;
    }

    private void OnLocaleChanged(Locale locale) => Refresh();

    private void OnDestroy()
    {
        if (_saveService != null) _saveService.SavesChanged -= Refresh;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        ReleasePreview();
    }
}
