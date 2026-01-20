using TMPro;
using UnityEngine;

public class LocalizationLabel : MonoBehaviour
{
    public TextMeshProUGUI Text;
    public string Key;

    private string _suffix = "";

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    public void SetSuffix(string suffix)
    {
        _suffix = suffix;
        UpdateText();
    }

    public void UpdateText()
    {
        if (Text != null && !string.IsNullOrEmpty(Key))
        {
            Text.text = LocalizationManager.GetTranslation(Key) + _suffix;
        }
    }
}