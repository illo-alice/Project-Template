using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class PauseMenuToggle : MonoBehaviour
{
    private PauseMenuScreen _screen;

    private bool _isShowing;
    public bool IsShowing => _isShowing;
    private bool _isBlocked = true;

    private InputAction _exit;
    private KeyBindingsParameter _bindings;
    private Session _session;

    [Inject]
    public void Construct(GameInputService input, KeyBindingsParameter bindings, PauseMenuScreen screen, Session session)
    {
        _screen = screen;
        _session = session;
        _bindings = bindings;
        _exit = input.Actions.FindAction("UI/Exit", true);
        if (isActiveAndEnabled) _exit.started += OnExit;
        Hide(instant: true);
    }

    private void OnEnable()
    {
        if (_exit != null) _exit.started += OnExit;
    }

    private void OnDisable()
    {
        if (_exit != null) _exit.started -= OnExit;
    }

    private void OnExit(InputAction.CallbackContext obj)
    {
        if (_bindings == null || !_bindings.BlocksMenuInput) Toggle();
    }
    
    public void Toggle()
    {
        if (_isBlocked || !_session.IsStarted) return;
        
        if (!_isShowing)
            Show();
        else
            Hide();
    }
    
    private void Show(bool instant = false)
    {
        _isShowing = true;
        _screen.Show(instant).Forget();
    }
    
    private void Hide(bool instant = false)
    {
        _isShowing = false;
        _screen.Hide(instant).Forget();
    }

    public void Block()
    {
        _isBlocked = true;
    }

    public void UnBlock()
    {
        if (_session.IsStarted) _isBlocked = false;
    }

    public void ResetAndBlock()
    {
        _isBlocked = true;
        Hide(instant: true);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
