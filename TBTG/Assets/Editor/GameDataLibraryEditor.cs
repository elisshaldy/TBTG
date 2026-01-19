using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameDataLibrary))]
public class GameDataLibraryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Малюємо стандартний інспектор
        DrawDefaultInspector();

        GameDataLibrary library = (GameDataLibrary)target;

        GUILayout.Space(20);
        
        // Малюємо велику гарну кнопку
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("REFRESH LIBRARY (Load all Configs)", GUILayout.Height(40)))
        {
            library.Refresh();
        }
    }
}
