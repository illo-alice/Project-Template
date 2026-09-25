using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinScreen : MonoBehaviour
{
    [SerializeField] private TMP_InputField _roomCode;
    [SerializeField] private Button _join;
    [SerializeField] private Button _back;

    [SerializeField] private CanvasFader _fader;
    
    public event Action<string> Join;
    public event Action Back;
    
    private void Awake()
    {
        _join.onClick.AddListener(OnJoin);
        _back.onClick.AddListener(OnBack);
    }
    
    public UniTask Show(bool instant = false) => _fader.Show(instant);
    public UniTask Hide(bool instant = false) => _fader.Hide(instant);
    public void SetInteractable(bool interactable) => _fader.SetInteractable(interactable);
    
    public void ShowVoid() => Show().Forget();

    private void OnJoin() => Join?.Invoke(_roomCode.text);
    private void OnBack() => Back?.Invoke();

    private void OnDestroy()
    {
        if (_join != null) _join.onClick.RemoveListener(OnJoin);
        if (_back != null) _back.onClick.RemoveListener(OnBack);
    }
}
