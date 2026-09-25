using UnityEngine;
using TMPro;

public class TextSetterFloat : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    
    public void SetText(float value)
    {
        textComponent.text = value.ToString("F0");
    }
    
    public void SetTextPercent(float value)
    {
        textComponent.text = (value * 100).ToString("F0");
    }
}