using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public enum BarType
    {
        LoadingBar,
        ProgressBar,
    }
    
    [Header("Show / Hide Settings")]
    [SerializeField] private CanvasFader _fader;
    
    [Header("Bars Settings")]
    [SerializeField] private ProgressBar _progressBar;
    [SerializeField] private LoadingBar _loadingBar;
    
    [Header("Status")] [SerializeField]
    private TMP_Text _status;
    
    public void SetStatus(string text)
    {
        _status.text = text;
    }
    
    public UniTask Show(bool instant = false, CancellationToken cancellationToken = default)
    {
        return _fader.Show(instant, cancellationToken);
    }

    public UniTask Hide(bool instant = false, CancellationToken cancellationToken = default)
    {
        return _fader.Hide(instant, cancellationToken);
    }

    public async UniTask SelectBar(BarType type, bool instant = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _progressBar.SetProgress(0f);
        
        if (type == BarType.LoadingBar)
        {
            await _loadingBar.Show(instant, cancellationToken);
        }
        else if (type == BarType.ProgressBar)
        {
            await _loadingBar.Hide(instant: true, cancellationToken: cancellationToken);
        }
    }
    
    public void SetProgress(float progress, bool instant = false)
    {
        _progressBar.SetProgress(progress, instant);
    }
}
