using UnityEngine;

public sealed class DisplaySettingsService
{
    private int _width = Screen.width;
    private int _height = Screen.height;
    private FullScreenMode _mode = Screen.fullScreenMode;
    private bool _isUpdating;
    private int _lastRequestFrame = -1;
    private int _lastRequestedWidth;
    private int _lastRequestedHeight;
    private FullScreenMode _lastRequestedMode;

    public void BeginUpdate()
    {
        _isUpdating = true;
        ReadCurrentDisplay();
    }

    public void EndUpdate(bool apply)
    {
        _isUpdating = false;
        if (apply)
            Apply();
        else
            ReadCurrentDisplay();
    }

    public void SetMode(FullScreenMode mode)
    {
        _mode = mode;
        Apply();
    }

    public void SetResolution(int width, int height)
    {
        _width = width;
        _height = height;
        Apply();
    }

    private void ReadCurrentDisplay()
    {
        _width = Screen.width;
        _height = Screen.height;
        _mode = Screen.fullScreenMode;
    }

    private void Apply()
    {
        if (_isUpdating || Application.isEditor)
            return;

        // Unity applies display changes at the end of the frame.
        if (_lastRequestFrame == Time.frameCount &&
            _lastRequestedWidth == _width && _lastRequestedHeight == _height && _lastRequestedMode == _mode)
            return;

        if (_lastRequestFrame != Time.frameCount &&
            Screen.width == _width && Screen.height == _height && Screen.fullScreenMode == _mode)
            return;

        Screen.SetResolution(_width, _height, _mode);
        _lastRequestFrame = Time.frameCount;
        _lastRequestedWidth = _width;
        _lastRequestedHeight = _height;
        _lastRequestedMode = _mode;
    }
}
