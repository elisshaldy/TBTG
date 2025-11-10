// Tile.cs
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridCoordinates;
    public TileType Type;

    [Header("Active Tile Data")]
    public TileEffectData CurrentEffect; // Змінний ефект для Active Tiles

    private Character _occupant = null;

    public bool IsOccupied => _occupant != null;
    public Character Occupant => _occupant;

    // Додати до скрипта Tile.cs
    void Start()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RegisterTile(GridCoordinates, this);
        }
    }

    public void SetOccupant(Character character)
    {
        _occupant = character;

        if (Type == TileType.ActiveTile && CurrentEffect != null)
        {
            // Відкрити жетон ефекту і застосувати початковий вплив
            CurrentEffect.ApplyInitialEffect(character);
            Debug.Log($"Active Tile at {GridCoordinates} revealed: {CurrentEffect.EffectName}");
        }
    }

    public void RemoveOccupant()
    {
        _occupant = null;
    }

    // Перевірка, чи можна пройти
    public bool IsPassable()
    {
        return Type != TileType.Impassable && !IsOccupied;
    }

    // Перевірка, чи можна атакувати через неї
    public bool IsAttackableThrough()
    {
        return Type != TileType.Impassable;
    }
}