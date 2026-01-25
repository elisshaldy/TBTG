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
    
    private void Start()
    {
        // Ensure text updates on start in case OnEnable ran before Manager init
        UpdateText(); 
    }

    private void OnValidate()
    {
        if (Text == null)
        {
            Text = GetComponent<TextMeshProUGUI>();
        }
    }

    public void SetSuffix(string suffix)
    {
        _suffix = suffix;
        UpdateText();
    }
    
    public void SetKey(string newKey)
    {
        Key = newKey;
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