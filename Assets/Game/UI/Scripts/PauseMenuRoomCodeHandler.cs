using System;
using UnityEngine;
using VContainer.Unity;

public sealed class PauseMenuRoomCodeHandler : IStartable, IDisposable
{
    private readonly PauseMenuScreen _screen;
    private readonly RoomCodeService _codes;
    public PauseMenuRoomCodeHandler(PauseMenuScreen pauseMenuScreen, RoomCodeService roomCodeService)
    {
        _screen = pauseMenuScreen;
        _codes = roomCodeService;
    }
    public void Start()
    {
        _screen.CopyCode += Copy;
        _codes.OnRoomCodeChanged += _screen.SetRoomCode;
        _screen.SetRoomCode(_codes.CurrentRoomCode);
    }
    public void Dispose()
    {
        _screen.CopyCode -= Copy;
        _codes.OnRoomCodeChanged -= _screen.SetRoomCode;
    }
    private void Copy()
    {
        if (string.IsNullOrEmpty(_codes.CurrentRoomCode)) return;
        GUIUtility.systemCopyBuffer = _codes.CurrentRoomCode;
        _screen.PlayCopiedAnimation();
    }
}
