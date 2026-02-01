using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridCoordinates;
    public TileType Type;

    private void Awake() // temp solution
    {
        // If coordinates are unassigned (0,0), calculate them from world position
        if (GridCoordinates == Vector2Int.zero)
        {
            float tileSize = 1f;
            var generator = FindObjectOfType<MapGenerator>();
            if (generator != null) tileSize = generator.TileSize;

            GridCoordinates = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / tileSize),
                Mathf.RoundToInt(transform.position.z / tileSize)
            );
        }
    }
}