using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour
{
    [SerializeField] private CanvasFader _fader;
    [SerializeField] private Image _first;
    [SerializeField] private Image _second;
    [SerializeField] private Image _third;
    
    private Sequence _loadingSequence;
    
    private void Awake()
    {
        _fader.Hide(instant: true).Forget();
    }
    
    public async UniTask Show(bool instant = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StopLoadingSequence();
        
        SetAlpha(_first, 0.2f);
        SetAlpha(_second, 0.2f);
        SetAlpha(_third, 0.2f);
        
        await _fader.Show(instant, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        
        _loadingSequence = DOTween.Sequence();

        var fadeTime = 0.4f;
        var delayBetweenDots = 0.15f;
        
        _loadingSequence.Insert(0.0f, _first.DOFade(1f, fadeTime).SetLoops(2, LoopType.Yoyo));
        _loadingSequence.Insert(delayBetweenDots, _second.DOFade(1f, fadeTime).SetLoops(2, LoopType.Yoyo));
        _loadingSequence.Insert(delayBetweenDots * 2f, _third.DOFade(1f, fadeTime).SetLoops(2, LoopType.Yoyo));
        
        var totalDuration = delayBetweenDots * 2f + fadeTime * 2f;
        _loadingSequence.AppendInterval(totalDuration + 0.1f);

        _loadingSequence.SetLoops(-1, LoopType.Restart);
    }

    public async UniTask Hide(bool instant = false, CancellationToken cancellationToken = default)
    {
        await _fader.Hide(instant, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        
        StopLoadingSequence();
    }

    private void StopLoadingSequence()
    {
        if (_loadingSequence != null && _loadingSequence.IsActive())
        {
            _loadingSequence.Kill();
        }
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        var c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private void OnDestroy()
    {
        StopLoadingSequence();
    }
}
