using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Game Data/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Visuals")]
    public string CharacterName;
    public Sprite CharacterSprite;
    
    [Header("Base Stats")]
    public int AttackBase = 0;
    public int DefenseBase = 0;
    
    public AttackGrid3x3 AttackPatternGrid;
}

[Serializable]
public class AttackGrid3x3
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