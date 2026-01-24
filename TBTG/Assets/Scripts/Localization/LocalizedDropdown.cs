using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class LocalizedDropdown : MonoBehaviour
{
    [Tooltip("Keys must match the order of options in the Dropdown")]
    public List<string> OptionKeys = new List<string>();

    private TMP_Dropdown _dropdown;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateLocalization;
        UpdateLocalization();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateLocalization;
    }

    public void UpdateLocalization()
    {
        if (_dropdown == null) return;
        
        // Ensure options count matches keys count to avoid errors, 
        // or just loop through min count
        int count = Mathf.Min(_dropdown.options.Count, OptionKeys.Count);

        for (int i = 0; i < count; i++)
        {
            string key = OptionKeys[i];
            if (!string.IsNullOrEmpty(key))
            {
                _dropdown.options[i].text = LocalizationManager.GetTranslation(key);
            }
        }

        // Refresh the currently selected text
        _dropdown.RefreshShownValue();
    }
}
