using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class CanvasFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField, Min(0f)] private float _showDuration = 0.2f;
    [SerializeField, Min(0f)] private float _hideDuration = 0.2f;
    [SerializeField] private bool _ignoreTimeScale;

    [SerializeField] private bool _showOnAwake;
    
    private CancellationTokenSource _fadeCancellation;
    private CancellationToken _destroyToken;
    private bool _initialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (this == null)
            throw new System.OperationCanceledException();

        if (_initialized)
            return;

        _destroyToken = destroyCancellationToken;
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_showOnAwake)
        {
            _canvasGroup.alpha = 1f;
            SetInteraction(true);
        }
        else
        {
            _canvasGroup.alpha = 0f;
            SetInteraction(false);
        }
        
        _initialized = true;
    }

    public UniTask Show(bool instant = false, CancellationToken cancellationToken = default)
    {
        return FadeTo(1f, instant ? 0f : _showDuration, cancellationToken);
    }

    public UniTask Hide(bool instant = false, CancellationToken cancellationToken = default)
    {
        return FadeTo(0f, instant ? 0f : _hideDuration, cancellationToken);
    }

    public void SetInteractable(bool interactable)
    {
        Initialize();
        _canvasGroup.interactable = interactable;
    }

    private async UniTask FadeTo(float alpha, float duration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Initialize();
        _destroyToken.ThrowIfCancellationRequested();
        CancelFade();
        
        if (alpha > 0f)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            SetInteraction(true);
        }

        if (duration <= 0f || _canvasGroup.alpha == alpha)
        {
            _canvasGroup.alpha = alpha;
            SetInteraction(alpha > 0f);
            return;
        }

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _destroyToken);
        _fadeCancellation = cancellation;

        try
        {
            await _canvasGroup.DOFade(alpha, duration)
                .SetUpdate(_ignoreTimeScale)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellation.Token);

            cancellation.Token.ThrowIfCancellationRequested();
            if (alpha == 0f)
                SetInteraction(false);
        }
        finally
        {
            if (_fadeCancellation == cancellation)
                _fadeCancellation = null;

            cancellation.Dispose();
        }
    }

    private void SetInteraction(bool enabled)
    {
        _canvasGroup.interactable = enabled;
        _canvasGroup.blocksRaycasts = enabled;
    }

    private void CancelFade()
    {
        var cancellation = _fadeCancellation;
        _fadeCancellation = null;
        cancellation?.Cancel();
    }

    private void Reset()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnDestroy()
    {
        CancelFade();
    }
}
