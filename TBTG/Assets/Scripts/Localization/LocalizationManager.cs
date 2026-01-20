using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    //public LocalizationConfig Config;
    public TMP_Dropdown PrimaryLanguageDropdown;

    private SystemLanguage currentLanguage = SystemLanguage.English;

    public static event UnityAction OnLanguageChanged;

    private const string LanguagePrefKey = "SelectedLanguage";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        int savedLangIndex = PlayerPrefs.GetInt(LanguagePrefKey, 0);
        SetLanguageFromDropdown(savedLangIndex);

        if (PrimaryLanguageDropdown != null)
        {
            PrimaryLanguageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            PrimaryLanguageDropdown.value = savedLangIndex;
            PrimaryLanguageDropdown.RefreshShownValue();
        }
    }

    private void OnDropdownValueChanged(int index)
    {
        SetLanguageFromDropdown(index);

        if (PrimaryLanguageDropdown != null && PrimaryLanguageDropdown.value != index)
        {
            PrimaryLanguageDropdown.value = index;
            PrimaryLanguageDropdown.RefreshShownValue();
        }
    }

    public void SetLanguage(SystemLanguage language)
    {
        currentLanguage = language;
        OnLanguageChanged?.Invoke();
    }

    public void SetLanguageFromDropdown(int index)
    {
        SystemLanguage language = index == 0 ? SystemLanguage.English : SystemLanguage.German;
        SetLanguage(language);

        PlayerPrefs.SetInt(LanguagePrefKey, index);
        PlayerPrefs.Save();
    }

    public static string GetTranslation(string key)
    {
        // if (Instance == null || Instance.Config == null)
        // {
        //     return key;
        // }
        //
        // string translation = Instance.Config.GetTranslation(key, Instance.currentLanguage);
        // return string.IsNullOrEmpty(translation) ? key : translation;
        return key;
    }

    public SystemLanguage GetCurrentLanguage()
    {
        return Instance != null ? Instance.currentLanguage : SystemLanguage.English;
    }
}