using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameInputService : IDisposable
{
    public InputActionAsset Actions { get; }
    public GameInputService(InputActionAsset asset)
    {
        if (asset == null) throw new ArgumentNullException(nameof(asset));
        Actions = UnityEngine.Object.Instantiate(asset);
        Actions.FindActionMap("UI", true).Enable();
        Actions.FindActionMap("Voice")?.Enable();
    }
    public void Dispose()
    {
        Actions.Disable();
        UnityEngine.Object.Destroy(Actions);
    }
}
