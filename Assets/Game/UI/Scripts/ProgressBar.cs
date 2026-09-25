using DG.Tweening;
using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private TMPFillSlider _slider;
    [SerializeField] private float _sliderDuration = 0.2f;

    private Tween _progressTween;

    public void SetProgress(float progress, bool instant = false)
    {
        _progressTween?.Kill();
        _progressTween = null;

        if (instant)
        {
            _slider.SetValue(progress);
            return;
        }

        _progressTween = _slider
            .DOValue(progress, _sliderDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        _progressTween?.Kill();
    }
}
