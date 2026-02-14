using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterData))]
public class CharacterDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("CharacterName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("CharacterDescription"));

        SerializedProperty spriteProp = serializedObject.FindProperty("CharacterSprite");
        EditorGUILayout.PropertyField(spriteProp);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_characterModel"));
        
        if (spriteProp.objectReferenceValue != null)
        {
            Sprite sprite = spriteProp.objectReferenceValue as Sprite;
            if (sprite != null)
            {
                Texture2D tex = sprite.texture;

                Rect rect = GUILayoutUtility.GetRect(185, 272, GUILayout.ExpandWidth(false));

                Rect uv = new Rect(
                    sprite.rect.x / tex.width,
                    sprite.rect.y / tex.height,
                    sprite.rect.width / tex.width,
                    sprite.rect.height / tex.height
                );

                GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AttackBase"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefenseBase"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AttackPatternGrid"));

        serializedObject.ApplyModifiedProperties();
    }
}