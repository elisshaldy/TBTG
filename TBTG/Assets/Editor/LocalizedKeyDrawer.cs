using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomPropertyDrawer(typeof(LocalizedKeyAttribute))]
public class LocalizedKeyDrawer : PropertyDrawer
{
    private static Dictionary<string, string> _cachedTranslations = new Dictionary<string, string>();
    private static long _lastFileWriteTime = 0;
    private const string LocalizationPath = "Assets/Localization/Localization.csv";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [LocalizedKey] with strings only.");
            return;
        }

        // Load localization if needed
        UpdateLocalizationIfNeeded();

        // Get translation
        string key = property.stringValue;
        string translation = "Key not found";
        
        if (!string.IsNullOrEmpty(key))
        {
            if (_cachedTranslations.TryGetValue(key, out string val))
            {
                translation = val;
            }
        }
        else
        {
            translation = "Empty key";
        }

        // Draw the original field
        Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(fieldRect, property, label);

        // Draw translation label
        float spacing = 2f;
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        style.wordWrap = true;
        style.fontStyle = FontStyle.Italic;

        float labelWidth = position.width - EditorGUIUtility.labelWidth;
        float labelHeight = style.CalcHeight(new GUIContent(translation), labelWidth);
        
        Rect labelRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y + EditorGUIUtility.singleLineHeight + spacing, labelWidth, labelHeight);
        
        EditorGUI.LabelField(labelRect, translation, style);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
            return EditorGUIUtility.singleLineHeight;

        UpdateLocalizationIfNeeded();
        
        string key = property.stringValue;
        string translation = "Key not found";
        if (!string.IsNullOrEmpty(key) && _cachedTranslations.TryGetValue(key, out string val))
            translation = val;
        else if (string.IsNullOrEmpty(key))
            translation = "Empty key";

        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.wordWrap = true;
        
        float labelWidth = EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 30; // 30 for padding/scrollbar
        float labelHeight = style.CalcHeight(new GUIContent(translation), labelWidth);

        return EditorGUIUtility.singleLineHeight + labelHeight + 5f; 
    }

    private void UpdateLocalizationIfNeeded()
    {
        string fullPath = Path.Combine(Application.dataPath, "..", LocalizationPath);
        if (!File.Exists(fullPath)) return;

        long currentWriteTime = File.GetLastWriteTime(fullPath).Ticks;
        if (currentWriteTime == _lastFileWriteTime && _cachedTranslations.Count > 0) return;

        LoadLocalization(fullPath);
        _lastFileWriteTime = currentWriteTime;
    }

    private void LoadLocalization(string path)
    {
        _cachedTranslations.Clear();
        try
        {
            string csvText = File.ReadAllText(path);
            List<List<string>> rows = ParseCsvText(csvText);

            if (rows.Count == 0) return;

            // Header: KEY,Українська,English
            // Assume Col 0 is KEY, Col 1 is Ukrainian
            for (int i = 1; i < rows.Count; i++)
            {
                List<string> row = rows[i];
                if (row.Count < 2) continue;

                string key = row[0].Trim();
                string ukr = row[1]; // Keep whitespace if any, or trim? Translation might need spaces.
                
                if (!string.IsNullOrEmpty(key) && !_cachedTranslations.ContainsKey(key))
                {
                    _cachedTranslations[key] = ukr;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LocalizedKeyDrawer] Failed to load localization: {e.Message}");
        }
    }

    // Reuse parsing logic from LocalizationManager but simplified
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
}
