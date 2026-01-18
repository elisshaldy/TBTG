using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;

public static class GoogleSheetsDownloader
{
    private static UnityWebRequest _currentRequest;

    [MenuItem("Localization/Download Google Sheet")]
    public static void DownloadSelectedSheet()
    {
        GoogleSheetsSettings settings = GetSelectedSettings();
        if (settings == null)
        {
            Debug.LogError("Please select a GoogleSheetsSettings asset first.");
            return;
        }

        Download(settings);
    }

    private static GoogleSheetsSettings _activeSettings;

    public static void Download(GoogleSheetsSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SpreadsheetId))
        {
            Debug.LogError("Spreadsheet ID is empty!");
            return;
        }

        _activeSettings = settings;
        string url = settings.GetDownloadUrl();
        Debug.Log($"<color=green>Google Sheets:</color> Downloading from: {url}");

        _currentRequest = UnityWebRequest.Get(url);
        _currentRequest.SendWebRequest();

        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (_currentRequest != null && _currentRequest.isDone)
        {
            EditorApplication.update -= Update;

            if (_currentRequest.result == UnityWebRequest.Result.Success)
            {
                ProcessDownload(_currentRequest.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"<color=red>Google Sheets Error:</color> {_currentRequest.error}");
            }

            _currentRequest.Dispose();
            _currentRequest = null;
        }
    }

    private static void ProcessDownload(string csvData)
    {
        string path = _activeSettings != null ? _activeSettings.LocalPath : "";
        
        if (string.IsNullOrEmpty(path))
        {
            path = EditorUtility.SaveFilePanel("Save CSV", "Assets", "GoogleSheetData.csv", "csv");
        }
        else
        {
            // Convert relative to absolute for File.WriteAllText
            if (path.StartsWith("Assets"))
            {
                path = Path.Combine(Application.dataPath, path.Substring(7));
            }
        }

        if (!string.IsNullOrEmpty(path))
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(path, csvData);
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>Google Sheets:</color> Successfully downloaded to {path}");
            
            // Highlight the file
            string relativePath = path;
            if (path.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + path.Substring(Application.dataPath.Length);
            }
            var asset = AssetDatabase.LoadMainAssetAtPath(relativePath);
            if (asset != null) EditorGUIUtility.PingObject(asset);
        }
    }

    private static GoogleSheetsSettings GetSelectedSettings()
    {
        var settings = Selection.activeObject as GoogleSheetsSettings;
        if (settings != null) return settings;

        string[] guids = AssetDatabase.FindAssets("t:GoogleSheetsSettings");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GoogleSheetsSettings>(path);
        }

        return null;
    }
}
