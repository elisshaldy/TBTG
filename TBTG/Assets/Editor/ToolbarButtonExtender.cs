using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class ToolbarButtonExtender
{
    static ToolbarButtonExtender()
    {
        EditorApplication.delayCall += () => {
            InjectButton();
        };
    }

    private static void InjectButton()
    {
        var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        var toolbarObjects = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbarObjects == null || toolbarObjects.Length == 0) return;

        var toolbar = toolbarObjects[0];
        var rootField = toolbarType.GetField("m_RootVisualElement", BindingFlags.NonPublic | BindingFlags.Instance);
        if (rootField == null) return;
        
        var root = (VisualElement)rootField.GetValue(toolbar);
        if (root == null) return;

        // In Windows/newer Unity versions, the structure might be different
        var leftToolbar = root.Q("ToolbarZoneLeftAlign") ?? root.Q("ToolbarZoneLeft");
        if (leftToolbar == null)
        {
            // Fallback: try to find any suitable container if the specific ID isn't found
            leftToolbar = root.ElementAt(0); 
        }
        
        if (leftToolbar == null) return;

        // Check if button already exists
        if (leftToolbar.Q("google-sheets-button") != null) return;

        var button = new Button(() => {
            GoogleSheetsDownloader.DownloadSelectedSheet();
        })
        {
            name = "google-sheets-button",
            tooltip = "Download Google Spreadsheet",
            style = {
                width = 40,
                height = 22,
                marginLeft = 10,
                marginRight = 5,
                marginTop = 2,
                backgroundColor = new StyleColor(new Color(0.12f, 0.45f, 0.23f, 1f)), // Google Sheets Green
                borderTopLeftRadius = 4,
                borderTopRightRadius = 4,
                borderBottomLeftRadius = 4,
                borderBottomRightRadius = 4,
                borderLeftWidth = 0,
                borderRightWidth = 0,
                borderTopWidth = 0,
                borderBottomWidth = 0,
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                justifyContent = Justify.Center
            }
        };

        var label = new Label("Sheet") {
            style = {
                color = Color.white,
                fontSize = 10,
                unityFontStyleAndWeight = FontStyle.Bold
            }
        };
        button.Add(label);

        leftToolbar.Add(button);
    }
}
