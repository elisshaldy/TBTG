using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Linq;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    
    public TextAsset LocalizationFile;
    private TMP_Dropdown PrimaryLanguageDropdown;

    private string _currentLanguage;
    private List<string> _availableLanguages = new List<string>();
    
    // Dictionary<Key, Dictionary<LanguageName, Translation>>
    private Dictionary<string, Dictionary<string, string>> _translations = new Dictionary<string, Dictionary<string, string>>();

    public static event UnityAction OnLanguageChanged;

    private const string LanguagePrefKey = "SelectedLanguageName";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLocalization();
        InitializeDropdown();
    }

    private void LoadLocalization()
    {
        if (LocalizationFile == null)
        {
            Debug.LogError("[LocalizationManager] Localization CSV file is not assigned!");
            return;
        }

        ParseCSV(LocalizationFile.text);
    }

    private void ParseCSV(string csvText)
    {
        _translations.Clear();
        _availableLanguages.Clear();

        List<List<string>> rows = ParseCsvText(csvText);

        if (rows.Count == 0) return;

        // Parse Header
        List<string> header = rows[0];
        if (header.Count < 2)
        {
            Debug.LogError("[LocalizationManager] CSV header is too short.");
            return;
        }

        // Assume Column 0 is "KEY", rest are Languages
        // We start from 1
        for (int i = 1; i < header.Count; i++)
        {
            string lang = header[i].Trim();
            if (!string.IsNullOrEmpty(lang))
            {
                _availableLanguages.Add(lang);
            }
        }

        // Parse Data
        for (int i = 1; i < rows.Count; i++)
        {
            List<string> row = rows[i];
            
            // Need at least 2 columns (KEY + 1 Lang)
            if (row.Count < 2) continue;

            string key = row[0].Trim();
            if (string.IsNullOrEmpty(key)) continue;

            if (!_translations.ContainsKey(key))
            {
                _translations[key] = new Dictionary<string, string>();
            }

            // Map columns to languages
            // row[colIndex] for colIndex 1..row.Count
            // mapped to _availableLanguages[colIndex - 1]
            for (int colIndex = 1; colIndex < row.Count; colIndex++)
            {
                // Ensure we don't go out of bounds if row has more cols than header for some reason, 
                // or safely map to available languages
                int langIndex = colIndex - 1;
                if (langIndex < _availableLanguages.Count)
                {
                    string langName = _availableLanguages[langIndex];
                    _translations[key][langName] = row[colIndex];
                }
            }
        }
        
        Debug.Log($"[LocalizationManager] Loaded {_translations.Count} keys and {_availableLanguages.Count} languages: {string.Join(", ", _availableLanguages)}");
    }

    private void InitializeDropdown()
    {
        if (_availableLanguages.Count == 0) return;

        // Load saved language or default to the first one available
        string defaultLang = _availableLanguages.Contains("English") ? "English" : _availableLanguages[0];
        string savedLang = PlayerPrefs.GetString(LanguagePrefKey, defaultLang);

        // Ensure saved lang still exists
        if (!_availableLanguages.Contains(savedLang))
        {
            savedLang = _availableLanguages[0];
        }
        
        _currentLanguage = savedLang;

        // If a dropdown was assigned in inspector (Scene 1), register it
        if (PrimaryLanguageDropdown != null)
        {
            RegisterDropdown(PrimaryLanguageDropdown);
        }
        
        // Trigger initial update
        OnLanguageChanged?.Invoke();
    }

    /// <summary>
    /// Call this from any Scene to link a Dropdown to the LocalizationManager
    /// </summary>
    public void RegisterDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;
        if (_availableLanguages.Count == 0) return;

        dropdown.ClearOptions();
        dropdown.AddOptions(_availableLanguages);

        int index = _availableLanguages.IndexOf(_currentLanguage);
        if (index < 0) index = 0;
        
        dropdown.value = index;
        dropdown.RefreshShownValue();
        
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener((val) => 
        {
            if (val >= 0 && val < _availableLanguages.Count)
            {
                SetLanguage(_availableLanguages[val]);
            }
        });
    }

    public List<string> GetAvailableLanguages()
    {
        return _availableLanguages;
    }

    private List<List<string>> ParseCsvText(string text)
    {
        var result = new List<List<string>>();
        
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        bool inQuotes = false;
        string currentField = "";
        var currentRow = new List<string>();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    currentField += '"';
                    i++; 
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                currentRow.Add(currentField);
                currentField = "";
            }
            else if (c == '\n' && !inQuotes)
            {
                currentRow.Add(currentField);
                result.Add(new List<string>(currentRow));
                currentRow.Clear();
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        if (!string.IsNullOrEmpty(currentField) || currentRow.Count > 0)
        {
            currentRow.Add(currentField);
            result.Add(currentRow);
        }

        return result;
    }

    // Removed OnDropdownValueChanged as we now use lambda in RegisterDropdown
    
    public void SetLanguage(string languageName)
    {
        if (_currentLanguage == languageName) return;
        
        if (_availableLanguages.Contains(languageName))
        {
            _currentLanguage = languageName;
            PlayerPrefs.SetString(LanguagePrefKey, _currentLanguage);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }
    }

    public static string GetTranslation(string key)
    {
        if (Instance == null) return key;

        if (Instance._translations.TryGetValue(key, out var langDict))
        {
            // Try get current language
            if (langDict.TryGetValue(Instance._currentLanguage, out string val) && !string.IsNullOrEmpty(val))
            {
                return val;
            }

            return key + " <MISSING>";
        }

        return key;
    }
    
    public string GetCurrentLanguage()
    {
        return _currentLanguage;
    }
}