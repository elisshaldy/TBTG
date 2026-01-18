using UnityEditor;
using UnityEngine;

public class GoogleSheetsWindow : EditorWindow
{
    private GoogleSheetsSettings _settings;

    [MenuItem("Localization/Open Downloader")]
    public static void ShowWindow()
    {
        GetWindow<GoogleSheetsWindow>("Google Sheets");
    }

    private void OnEnable()
    {
        FindSettings();
    }

    private void FindSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:GoogleSheetsSettings");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _settings = AssetDatabase.LoadAssetAtPath<GoogleSheetsSettings>(path);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Google Sheets Downloader", EditorStyles.boldLabel);

        _settings = (GoogleSheetsSettings)EditorGUILayout.ObjectField("Settings Asset", _settings, typeof(GoogleSheetsSettings), false);

        if (_settings == null)
        {
            if (GUILayout.Button("Create New Settings"))
            {
                CreateNewSettings();
            }
            EditorGUILayout.HelpBox("Please select or create a GoogleSheetsSettings asset.", MessageType.Warning);
            return;
        }

        Editor.CreateEditor(_settings).OnInspectorGUI();

        GUILayout.Space(20);

        if (GUILayout.Button("Download CSV Now", GUILayout.Height(40)))
        {
            GoogleSheetsDownloader.Download(_settings);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.HelpBox("Make sure your Google Sheet is set to 'Anyone with the link can view' or is published to the web.", MessageType.Info);
    }

    private void CreateNewSettings()
    {
        GoogleSheetsSettings asset = ScriptableObject.CreateInstance<GoogleSheetsSettings>();
        AssetDatabase.CreateAsset(asset, "Assets/GoogleSheetsSettings.asset");
        AssetDatabase.SaveAssets();
        _settings = asset;
        Selection.activeObject = asset;
    }
}
