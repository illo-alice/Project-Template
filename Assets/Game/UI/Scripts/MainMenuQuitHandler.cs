using System;
using UnityEngine;
using VContainer.Unity;

public class MainMenuQuitHandler : IStartable, IDisposable
{
    private readonly MainMenuScreen _mainMenuScreen;
    
    public MainMenuQuitHandler(MainMenuScreen mainMenuScreen)
    {
        _mainMenuScreen = mainMenuScreen;
    }
    
    public void Start() => _mainMenuScreen.Quit += OnQuit;

    private void OnQuit()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Dispose() => _mainMenuScreen.Quit -= OnQuit;
}
