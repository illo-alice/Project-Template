using System;
using UnityEngine;
using UnityEngine.UI;

public class CreditsScreen : MonoBehaviour
{
    [SerializeField] private CanvasFader _fader;
    [SerializeField] private Button _back;

    public Action Back;

    private void Awake()
    {
        _back.onClick.AddListener(() => Back?.Invoke());
    }

    public void ShowInstant()
    {
        _fader.Show(true);
    }
    
    public void HideInstant()
    {
        _fader.Hide(true);
    }
    
    public void Show()
    {
        _fader.Show();
    }
    
    public void Hide()
    {
        _fader.Hide();
    }
}
