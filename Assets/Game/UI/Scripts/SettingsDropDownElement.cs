using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SettingsDropDownElement : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _dropdown;
    
    private Action<int> _setValue;
    private Action _refresh;
    private Action _unsubscribe;

    public void Bind(IChoiceSettingsParameter parameter)
    {
        if (parameter == null)
            throw new ArgumentNullException(nameof(parameter));

        Unbind();
        // Keep IDs alongside the displayed snapshot. Only TMP_Dropdown uses
        // indices; the parameter and save file receive the stable ID.
        var choices = new List<SettingsChoice>();

        void Refresh(string id)
        {
            var index = choices.FindIndex(choice => string.Equals(choice.Id, id, StringComparison.Ordinal));
            _dropdown.SetValueWithoutNotify(index);
            _dropdown.RefreshShownValue();
            _dropdown.interactable = index >= 0;
        }

        void Rebuild()
        {
            choices.Clear();
            choices.AddRange(parameter.Options);
            var labels = new List<string>(choices.Count);
            foreach (var choice in choices)
                labels.Add(choice.Label);

            _dropdown.ClearOptions();
            _dropdown.AddOptions(labels);
            Refresh(parameter.Value);
        }

        _setValue = index =>
        {
            if (index < 0 || index >= choices.Count)
                return;

            try
            {
                parameter.SetValue(choices[index].Id);
            }
            finally
            {
                Refresh(parameter.Value);
            }
        };
        _refresh = () => Refresh(parameter.Value);
        parameter.Changed += Refresh;
        parameter.OptionsChanged += Rebuild;
        _unsubscribe = () =>
        {
            parameter.Changed -= Refresh;
            parameter.OptionsChanged -= Rebuild;
        };
        Rebuild();
    }
    
    public void Bind<T>(IEnumSettingsParameter<T> parameter, Func<T, string> getLabel = null) where T : Enum
    {
        if (parameter == null)
            throw new ArgumentNullException(nameof(parameter));

        Unbind();
        var values = (T[])Enum.GetValues(typeof(T));
        var labels = new List<string>(values.Length);
        foreach (var value in values)
            labels.Add(getLabel != null ? getLabel(value) : value.ToString());

        _dropdown.ClearOptions();
        _dropdown.AddOptions(labels);
        _dropdown.interactable = values.Length > 0;

        void Refresh(T value)
        {
            _dropdown.SetValueWithoutNotify(Array.IndexOf(values, value));
            _dropdown.RefreshShownValue();
        }

        _setValue = index =>
        {
            if (index < 0 || index >= values.Length)
                return;

            try
            {
                parameter.SetValue(values[index]);
            }
            finally
            {
                Refresh(parameter.Value);
            }
        };
        _refresh = () => Refresh(parameter.Value);
        parameter.Changed += Refresh;
        _unsubscribe = () => parameter.Changed -= Refresh;
        _refresh();
    }

    public void Unbind()
    {
        _unsubscribe?.Invoke();
        _unsubscribe = null;
        _refresh = null;
        _setValue = null;
    }

    private void OnEnable()
    {
        _dropdown.onValueChanged.AddListener(DropdownOnValueChanged);
        _refresh?.Invoke();
    }

    private void OnDisable()
    {
        _dropdown.onValueChanged.RemoveListener(DropdownOnValueChanged);
    }

    private void OnDestroy() => Unbind();

    private void DropdownOnValueChanged(int index) => _setValue?.Invoke(index);
}
