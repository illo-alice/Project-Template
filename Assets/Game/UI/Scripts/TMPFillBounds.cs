using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPFillBounds : MonoBehaviour
{
    private static readonly int TextLeft =
        Shader.PropertyToID("_TextLeft");

    private static readonly int TextRight =
        Shader.PropertyToID("_TextRight");

    private TextMeshProUGUI _text;
    private Material _material;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _material = _text.fontMaterial;
    }

    private void OnEnable()
    {
        _text.OnPreRenderText += UpdateBounds;
        _text.ForceMeshUpdate();
    }

    private void OnDisable()
    {
        _text.OnPreRenderText -= UpdateBounds;
    }

    private void UpdateBounds(TMP_TextInfo textInfo)
    {
        var bounds = _text.textBounds;

        var left = bounds.min.x;
        var right = bounds.max.x;
        
        right = Mathf.Max(right, left + 0.001f);

        _material.SetFloat(TextLeft, left);
        _material.SetFloat(TextRight, right);
    }
}