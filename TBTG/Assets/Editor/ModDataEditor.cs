using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ModData))]
public class ModDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("ModificatorName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ModificatorDescription"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("ModType"));
        
        ModType modType = (ModType)serializedObject.FindProperty("ModType").enumValueIndex;
        
        if (modType != ModType.Critical)
        {
            SerializedProperty backgroundProp = serializedObject.FindProperty("Background");
            EditorGUILayout.PropertyField(backgroundProp);
            DrawSpritePreview(backgroundProp, 192, 64);

            SerializedProperty iconProp = serializedObject.FindProperty("Icon");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Icon", EditorStyles.label);

            EditorGUILayout.PropertyField(iconProp, GUIContent.none);
            DrawSpritePreview(iconProp, 64, 64);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Price"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ReactiveParameters"), true);
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Critical"), true);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSpritePreview(SerializedProperty prop, float width, float height)
    {
        if (prop.objectReferenceValue != null)
        {
            Sprite sprite = prop.objectReferenceValue as Sprite;
            if (sprite != null)
            {
                Texture2D tex = sprite.texture;
                Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
                EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.2f));
                
                Rect uv = new Rect(
                    sprite.rect.x / tex.width,
                    sprite.rect.y / tex.height,
                    sprite.rect.width / tex.width,
                    sprite.rect.height / tex.height
                );

                GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
            }
        }
    }
}
