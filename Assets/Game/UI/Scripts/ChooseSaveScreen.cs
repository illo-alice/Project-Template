using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChooseSaveScreen : MonoBehaviour
{
    [SerializeField] private CanvasFader _fader;
    [SerializeField] private Button _choose;
    [SerializeField] private Button _back;
    [SerializeField] private Button _delete;

    public event Action Choose;
    public event Action Back;
    public event Action Delete;

    private void Awake()
    {
        _choose.onClick.AddListener(OnChoose);
        _back.onClick.AddListener(OnBack);
        _delete.onClick.AddListener(OnDelete);
    }

    public UniTask Show(bool instant = false) => _fader.Show(instant);
    public UniTask Hide(bool instant = false) => _fader.Hide(instant);
    public void SetInteractable(bool interactable) => _fader.SetInteractable(interactable);
    public void SetDeleteInteractable(bool interactable) => _delete.interactable = interactable;

    private void OnChoose() => Choose?.Invoke();
    private void OnBack() => Back?.Invoke();
    private void OnDelete() => Delete?.Invoke();

    private void OnDestroy()
    {
        if (_choose != null) _choose.onClick.RemoveListener(OnChoose);
        if (_back != null) _back.onClick.RemoveListener(OnBack);
        if (_delete != null) _delete.onClick.RemoveListener(OnDelete);
    }
}
