using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AttackGrid3x3))]
public class AttackGrid3x3Drawer : PropertyDrawer
{
    private const int GridSize = 3;
    private const float CellSize = 30f;
    private const float Padding = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight +
               (CellSize + Padding) * GridSize + 6f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.LabelField(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            label
        );

        SerializedProperty charPos =
            property.FindPropertyRelative("CharacterPosition");
        SerializedProperty cells =
            property.FindPropertyRelative("Cells");

        Vector2Int cp = charPos.vector2IntValue;

        float startY = position.y + EditorGUIUtility.singleLineHeight + 4f;
        float startX = position.x;

        Event e = Event.current;

        for (int y = 0; y < GridSize; y++)
        {
            for (int x = 0; x < GridSize; x++)
            {
                int index = y * GridSize + x;

                Rect cellRect = new Rect(
                    startX + x * (CellSize + Padding),
                    startY + y * (CellSize + Padding),
                    CellSize,
                    CellSize
                );

                bool isCharacter = (cp.x == x && cp.y == y);
                
                if (isCharacter)
                {
                    EditorGUI.DrawRect(cellRect, Color.black);
                }
                else
                {
                    bool value = cells.GetArrayElementAtIndex(index).boolValue;
                    EditorGUI.DrawRect(cellRect, value ? Color.red : Color.gray);
                }

                // 🖱 Інпут
                if (cellRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
                {
                    // ПКМ — перемістити персонажа
                    if (e.button == 1)
                    {
                        charPos.vector2IntValue = new Vector2Int(x, y);
                        cells.GetArrayElementAtIndex(index).boolValue = false;
                        e.Use();
                    }
                    // ЛКМ — toggle attack
                    else if (e.button == 0 && !isCharacter)
                    {
                        SerializedProperty cell =
                            cells.GetArrayElementAtIndex(index);
                        cell.boolValue = !cell.boolValue;
                        e.Use();
                    }
                }
            }
        }
    }

    private GUIStyle CenteredStyle(Color color)
    {
        return new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = color },
            fontStyle = FontStyle.Bold
        };
    }
}