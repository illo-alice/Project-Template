using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class HostGameScreen : MonoBehaviour
{
    [SerializeField] private CanvasFader _fader;

    public UniTask Show(bool instant = false) => _fader.Show(instant);
    public UniTask Hide(bool instant = false) => _fader.Hide(instant);
    public void SetInteractable(bool interactable) => _fader.SetInteractable(interactable);
}
