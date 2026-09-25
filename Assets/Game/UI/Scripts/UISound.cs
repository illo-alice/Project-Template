using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

/// <summary>Plays UI feedback for pointer, keyboard and gamepad interactions.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class UISound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private SoundDefinition _clickSound;
    [SerializeField] private SoundDefinition _hoverSound;

    private AudioService _audio;
    private Selectable _selectable;
    private Button _button;
    private bool _pointerInside;
    private int _lastHoverFrame = -1;

    [Inject]
    public void Construct(AudioService audio) => _audio = audio;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        _button = _selectable as Button;
        if (_button != null) _button.onClick.AddListener(PlayClick);
    }

    private void OnDisable() => _pointerInside = false;

    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(PlayClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_pointerInside) return;
        _pointerInside = true;
        PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData) => _pointerInside = false;

    public void OnSelect(BaseEventData eventData)
    {
        // Pointer selection already has hover feedback. Initial/programmatic
        // selection carries BaseEventData and should not make a navigation sound.
        if (eventData is AxisEventData && !_pointerInside) PlayHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Some Button variants replace the Button with a Toggle. Avoid listening
        // to onValueChanged, which also runs during settings initialization.
        if (_selectable is Toggle && eventData.button == PointerEventData.InputButton.Left)
            PlayClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (_selectable is Toggle) PlayClick();
    }

    private bool CanPlay => isActiveAndEnabled && _audio != null &&
        _selectable != null && _selectable.IsActive() && _selectable.IsInteractable();

    private void PlayClick()
    {
        if (CanPlay && _clickSound != null) _audio.Play2D(_clickSound, loop: false);
    }

    private void PlayHover()
    {
        if (!CanPlay || _hoverSound == null || _lastHoverFrame == Time.frameCount) return;
        _lastHoverFrame = Time.frameCount;
        _audio.Play2D(_hoverSound, loop: false);
    }
}
