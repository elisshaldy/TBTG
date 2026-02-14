using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Map Settings")]
    public int MapWidth = 9;
    public int MapHeight = 9;
    
    private Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();
    
    public IEnumerable<Vector2Int> GetAllRegisteredCoords()
    {
        return _tiles.Keys;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    public void RegisterTile(Vector2Int coords, Tile tile)
    {
        if (!_tiles.ContainsKey(coords))
        {
            _tiles.Add(coords, tile);
        }
    }

    public Tile GetTile(Vector2Int coords)
    {
        if (_tiles.TryGetValue(coords, out Tile tile))
        {
            return tile;
        }
        return null;
    }

    public List<Tile> GetTilesByOwner(int ownerID)
    {
        List<Tile> result = new List<Tile>();
        foreach (var tile in _tiles.Values)
        {
            if (tile != null && tile.PlacementOwnerID == ownerID)
                result.Add(tile);
        }
        return result;
    }
}