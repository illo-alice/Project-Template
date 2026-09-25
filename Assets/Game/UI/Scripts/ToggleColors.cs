using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Toggle))]
public class ToggleColors : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;
    [SerializeField] private Image _background;
    [SerializeField] private TMP_Text _label;

    [Header("Normal")]
    [SerializeField] private Color _normalBackground = Color.white;
    [SerializeField] private Color _normalText = new(0.196f, 0.196f, 0.196f, 1f);

    [Header("Selected")]
    [SerializeField] private Color _selectedBackground = new(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _selectedText = Color.white;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float _duration = 0.2f;
    [SerializeField] private Ease _ease = Ease.OutCubic;

    private Sequence _transition;

    private void Awake()
    {
        if (_toggle == null)
            _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        _toggle.onValueChanged.AddListener(OnValueChanged);
        Apply(_toggle.isOn, instant: true);
    }

    private void OnDisable()
    {
        _toggle.onValueChanged.RemoveListener(OnValueChanged);
        _transition?.Kill();
    }

    private void OnValueChanged(bool selected)
    {
        Apply(selected);
    }

    private void Apply(bool selected, bool instant = false)
    {
        // Continue a new transition from the current colors when switching quickly.
        _transition?.Kill();

        var backgroundColor = selected ? _selectedBackground : _normalBackground;
        var textColor = selected ? _selectedText : _normalText;

        if (instant || _duration <= 0f)
        {
            _background.color = backgroundColor;
            _label.color = textColor;
            return;
        }

        _transition = DOTween.Sequence()
            .Join(_background.DOColor(backgroundColor, _duration).SetEase(_ease))
            .Join(_label.DOColor(textColor, _duration).SetEase(_ease))
            .SetUpdate(true)
            .OnKill(() => _transition = null);
    }

    private void Reset()
    {
        _toggle = GetComponent<Toggle>();
        _background = GetComponent<Image>();
        _label = GetComponentInChildren<TMP_Text>(true);
    }
}
