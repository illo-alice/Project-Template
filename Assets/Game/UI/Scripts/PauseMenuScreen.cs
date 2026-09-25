using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenuScreen : MonoBehaviour
{
    [SerializeField] private CanvasFader _fader;
    [SerializeField] private Button _continue;
    [SerializeField] private Button _invite;
    [SerializeField] private Button _settings;
    [SerializeField] private Button _leave;
    [SerializeField] private Button _copyCode;

    public event Action Continue;
    public event Action Invite;
    public event Action Settings;
    public event Action Leave;
    public event Action CopyCode;
    private string _roomCode;

    private void Awake()
    {
        _continue.onClick.AddListener(OnContinue);
        _invite.onClick.AddListener(OnInvite);
        _settings.onClick.AddListener(OnSettings);
        _leave.onClick.AddListener(OnLeave);
        _copyCode.onClick.AddListener(OnCopyCode);
    }

    public UniTask Show(bool instant = false) => _fader.Show(instant);
    public UniTask Hide(bool instant = false) => _fader.Hide(instant);
    public void SetInviteAvailable(bool available) => _invite.interactable = available;
    public void SetInteractable(bool interactable) => _fader.SetInteractable(interactable);

    private void OnContinue() => Continue?.Invoke();
    private void OnInvite() => Invite?.Invoke();
    private void OnSettings() => Settings?.Invoke();
    private void OnLeave() => Leave?.Invoke();
    private void OnCopyCode() => CopyCode?.Invoke();

    public void SetRoomCode(string roomCode)
    {
        _roomCode = roomCode ?? string.Empty;
        _copyCode.GetComponentInChildren<TMP_Text>().text = _roomCode;
        _copyCode.interactable = !string.IsNullOrEmpty(_roomCode);
    }

    private async UniTask PlayCopied()
    {
        isCopiedPlaying = true;
        var text = _copyCode.GetComponentInChildren<TMP_Text>();
        try
        {
            text.text = S.UI("room_code_copied");
            await UniTask.Delay(500, ignoreTimeScale: true, cancellationToken: destroyCancellationToken);
            text.text = _roomCode;
        }
        finally { isCopiedPlaying = false; }
    }

    private bool isCopiedPlaying;
    
    public void PlayCopiedAnimation()
    {
        if (isCopiedPlaying) return;
        
        PlayCopied().Forget();
    }
}
