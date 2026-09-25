using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsSliderElement : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_InputField _valueInput;
    [SerializeField] private bool _showPercent = true;
    [SerializeField] private string _maxValueLabel;

    private IFloatSettingsParameter _parameter;
    private bool _subscribed;

    public float Value => _slider.value;

    public void Bind(IFloatSettingsParameter parameter)
    {
        if (parameter == null)
            throw new ArgumentNullException(nameof(parameter));

        Unbind();
        _parameter = parameter;
        if (isActiveAndEnabled)
            SubscribeParameter();

        SetValueWithoutNotify(_parameter.Value);
    }

    public void Unbind()
    {
        UnsubscribeParameter();
        _parameter = null;
    }

    public void SetValueWithoutNotify(float value)
    {
        _slider.SetValueWithoutNotify(value);
        UpdateValueInput();
    }

    private void OnEnable()
    {
        _slider.onValueChanged.AddListener(OnSliderValueChanged);
        _valueInput.onEndEdit.AddListener(OnInputEndEdit);
        SubscribeParameter();
        SetValueWithoutNotify(_parameter != null ? _parameter.Value : _slider.value);
    }

    private void OnDisable()
    {
        _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        _valueInput.onEndEdit.RemoveListener(OnInputEndEdit);
        UnsubscribeParameter();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void SubscribeParameter()
    {
        if (_parameter == null || _subscribed)
            return;

        _parameter.Changed += SetValueWithoutNotify;
        _subscribed = true;
    }

    private void UnsubscribeParameter()
    {
        if (!_subscribed)
            return;

        _parameter.Changed -= SetValueWithoutNotify;
        _subscribed = false;
    }

    private void OnSliderValueChanged(float value)
    {
        ApplyValue(value);
    }

    private void OnInputEndEdit(string text)
    {
        string number = (text ?? string.Empty).Trim().Replace(',', '.');
        if (!_valueInput.wasCanceled && !string.IsNullOrEmpty(_maxValueLabel) &&
            string.Equals(number, _maxValueLabel, StringComparison.OrdinalIgnoreCase))
        {
            ApplyValue(_slider.maxValue);
            return;
        }

        if (_valueInput.wasCanceled ||
            !float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
            float.IsNaN(value) || float.IsInfinity(value))
        {
            SetValueWithoutNotify(_parameter != null ? _parameter.Value : _slider.value);
            return;
        }

        ApplyValue(_showPercent ? value / 100f : value);
    }

    private void ApplyValue(float value)
    {
        // Use the Slider's range and whole-number rules for both input paths.
        _slider.SetValueWithoutNotify(value);
        _parameter?.SetValue(_slider.value);
        SetValueWithoutNotify(_parameter != null ? _parameter.Value : _slider.value);
    }

    private void UpdateValueInput()
    {
        if (_valueInput == null)
            return;

        if (!string.IsNullOrEmpty(_maxValueLabel) && _slider.value == _slider.maxValue)
        {
            _valueInput.SetTextWithoutNotify(_maxValueLabel);
            return;
        }

        float displayValue = _showPercent ? _slider.value * 100f : _slider.value;
        _valueInput.SetTextWithoutNotify(displayValue.ToString("0.##", CultureInfo.CurrentCulture));
    }

    private void Reset()
    {
        _slider = GetComponentInChildren<Slider>(true);
        _valueInput = GetComponentInChildren<TMP_InputField>(true);
    }
}
