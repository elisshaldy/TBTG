// Tile.cs
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridCoordinates;
    public TileType Type;
    
    //public TileEffectData CurrentEffect;

    //private Character _occupant = null;


    //public Character Occupant => _occupant;

    void Start()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RegisterTile(GridCoordinates, this);
        }
    }


}