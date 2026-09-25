using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageBox : MonoBehaviour
{
    [SerializeField] private CanvasFader _fader;
    
    [SerializeField] private Button _firstButton;
    [SerializeField] private Button _secondButton;
    [SerializeField] private Button _centerButton;

    [SerializeField] private TMP_Text _firstButtonText;
    [SerializeField] private TMP_Text _secondButtonText;
    [SerializeField] private TMP_Text _centerButtonText;
    
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _description;
    
    private bool _isShowing;
    private CancellationToken _destroyToken;

    private void Awake()
    {
        _destroyToken = destroyCancellationToken;
        _fader.Hide(instant: true).Forget();
    }

    public enum Option
    {
        First,
        Second
    }

    public async UniTask<Option> Show(
        string title,
        string description,
        string firstOption,
        string secondOption = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (this == null)
            throw new System.OperationCanceledException();

        // Also supports calls while the component is inactive, before Awake.
        if (!_destroyToken.CanBeCanceled)
            _destroyToken = destroyCancellationToken;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _destroyToken);
        cancellationToken = cancellation.Token;
        cancellationToken.ThrowIfCancellationRequested();

        if (_isShowing)
            throw new System.InvalidOperationException("MessageBox is already open.");

        _isShowing = true;

        var completion = new UniTaskCompletionSource<Option>();

        void SelectFirst() => completion.TrySetResult(Option.First);
        void SelectSecond() => completion.TrySetResult(Option.Second);

        try
        {
            var hasSecondOption = !string.IsNullOrEmpty(secondOption);
            _title.text = title;
            _description.text = description;
            _firstButton.gameObject.SetActive(hasSecondOption);
            _secondButton.gameObject.SetActive(hasSecondOption);
            _centerButton.gameObject.SetActive(!hasSecondOption);
            _firstButtonText.text = firstOption;
            _secondButtonText.text = secondOption ?? "";
            _centerButtonText.text = firstOption;

            _firstButton.onClick.AddListener(SelectFirst);
            _secondButton.onClick.AddListener(SelectSecond);
            _centerButton.onClick.AddListener(SelectFirst);

            await _fader.Hide(instant: true, cancellationToken: cancellationToken);

            await _fader.Show(cancellationToken: cancellationToken);

            var option = await completion.Task.AttachExternalCancellation(cancellationToken);

            await _fader.Hide(cancellationToken: cancellationToken);

            return option;
        }
        finally
        {
            if (_firstButton != null)
                _firstButton.onClick.RemoveListener(SelectFirst);

            if (_secondButton != null)
                _secondButton.onClick.RemoveListener(SelectSecond);

            if (_centerButton != null)
                _centerButton.onClick.RemoveListener(SelectFirst);

            if (!cancellationToken.IsCancellationRequested && _fader != null)
                _fader.Hide(instant: true).Forget();

            _isShowing = false;
        }
    }
}
