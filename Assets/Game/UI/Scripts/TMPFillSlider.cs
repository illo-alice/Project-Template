using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPFillSlider : MonoBehaviour
{
    private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");
    private TextMeshProUGUI _text;
    private Material _material;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _material = _text.fontMaterial;
    }
    
    public void SetValue(float value)
    {
        _material.SetFloat(FillAmount, value);
    }
    
    public Tweener DOValue(float endValue, float duration)
    {
        return DOTween.To(
            () => _material.GetFloat(FillAmount),
            SetValue,
            endValue,
            duration
        ).SetTarget(this);
    }
}