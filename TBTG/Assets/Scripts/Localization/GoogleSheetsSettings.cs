using UnityEngine;

[CreateAssetMenu(fileName = "GoogleSheetsSettings", menuName = "Game Data/Google Sheets Settings")]
public class GoogleSheetsSettings : ScriptableObject
{
    public string SpreadsheetId;
    public string SheetName;
    public string Gid = "0";
    public string LocalPath = "Assets/Localization/Localization.csv";
    
    public string GetDownloadUrl()
    {
        if (SpreadsheetId.Contains("docs.google.com/spreadsheets/d/"))
        {
            // Extract ID from full URL
            string id = SpreadsheetId.Split("/d/")[1].Split('/')[0];
            return $"https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={Gid}";
        }
        return $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=csv&gid={Gid}";
    }
}
