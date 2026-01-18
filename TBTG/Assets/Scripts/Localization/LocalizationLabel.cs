using TMPro;
using UnityEngine;

public class LocalizationLabel : MonoBehaviour
{
    public TextMeshProUGUI Text;
    public string Key;

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText()
    {
        if (Text != null && !string.IsNullOrEmpty(Key))
        {
            Text.text = LocalizationManager.GetTranslation(Key);
        }
    }
}