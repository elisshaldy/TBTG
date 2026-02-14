using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridCoordinates;
    public TileType Type;
    public int PlacementOwnerID = -1; // -1 = no one, 1 = Player 1, 2 = Player 2

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

    private void Start()
    {
        // РЕЄСТРАЦІЯ У ГЛОБАЛЬНОМУ РЕГІСТРІ
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RegisterTile(GridCoordinates, this);
        }

        AssignPlacementID();
        UpdateObjectName();
    }

    public void AssignPlacementID()
    {
        int w = 9; int h = 9;
        if (GridManager.Instance != null)
        {
            w = GridManager.Instance.MapWidth;
            h = GridManager.Instance.MapHeight;
        }

        if (IsInsideZone(GridCoordinates, 1, w, h)) PlacementOwnerID = 1;
        else if (IsInsideZone(GridCoordinates, 2, w, h)) PlacementOwnerID = 2;
        else PlacementOwnerID = -1;
    }

    private bool IsInsideZone(Vector2Int coords, int playerNum, int w, int h)
    {
        int dx = (playerNum == 1) ? coords.x : (w - 1 - coords.x);
        int dy = (playerNum == 1) ? (h - 1 - coords.y) : coords.y;

        if (dy == 0) return dx >= 0 && dx <= 4;
        if (dy == 1) return dx >= 0 && dx <= 2;
        if (dy == 2) return dx >= 0 && dx <= 1;
        if (dy == 3) return dx == 0;
        if (dy == 4) return dx == 0;
        
        return false;
    }

    private void UpdateObjectName()
    {
        string ownerTag = PlacementOwnerID != -1 ? $" [P{PlacementOwnerID}]" : "";
        if (!name.Contains("[P")) name += ownerTag;
    }

    private void OnDrawGizmos()
    {
        if (PlacementOwnerID == -1) return;

        Gizmos.color = (PlacementOwnerID == 1) ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.2f, 0.3f);
    }
}