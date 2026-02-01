using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewMovementCard", menuName = "Game Data/Movement Card")]
public class MovementCard : ScriptableObject
{
    public MovementGrid3x3 MovementPatternGrid;
}

[Serializable]
public class MovementGrid3x3
{
    [HideInInspector] public Vector2Int CharacterPosition = new Vector2Int(1, 1);
    
    public bool[] Cells = new bool[9];

    public bool Get(int x, int y)
    {
        return Cells[y * 3 + x];
    }

    public void Set(int x, int y, bool value)
    {
        Cells[y * 3 + x] = value;
    }

    public bool IsCharacterTile(int x, int y)
    {
        return CharacterPosition.x == x && CharacterPosition.y == y;
    }
}