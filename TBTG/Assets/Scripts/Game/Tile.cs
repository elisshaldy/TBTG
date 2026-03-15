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

    private void OnMouseDown()
    {
        if (CharacterPlacementManager.Instance != null && InitiativeSystem.Instance != null && InitiativeSystem.Instance.IsFinalized)
        {
            if (CharacterPlacementManager.Instance.IsMovementModeActive)
            {
                CharacterPlacementManager.Instance.TryMoveActiveCharacter(GridCoordinates);
            }
            else if (CharacterPlacementManager.Instance.IsTileUnderAttack(GridCoordinates))
            {
                CharacterPlacementManager.Instance.TryAttackTile(GridCoordinates);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // 1. Draw Placement Zones (only during setup phase)
        bool isSetupPhase = true;
        if (Application.isPlaying && InitiativeSystem.Instance != null && InitiativeSystem.Instance.IsFinalized)
            isSetupPhase = false;

        if (isSetupPhase && PlacementOwnerID != -1)
        {
            Gizmos.color = (PlacementOwnerID == 1) ? new Color(0, 1, 0, 0.4f) : new Color(1, 0, 0, 0.4f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.2f, 0.25f);
        }

        // 2. Draw Patterns (only during active game)
        if (Application.isPlaying && !isSetupPhase && CharacterPlacementManager.Instance != null)
        {
            if (CharacterPlacementManager.Instance.IsMovementModeActive)
            {
                // MOVEMENT HIGHLIGHT (Green)
                if (CharacterPlacementManager.Instance.IsTileMovable(GridCoordinates))
                {
                    Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.8f); // Bright Green
                    Vector3 center = transform.position + Vector3.up * 0.05f;
                    Vector3 size = new Vector3(0.95f, 0.1f, 0.95f);
                    Gizmos.DrawWireCube(center, size);
                    Gizmos.color = new Color(0, 1, 0, 0.2f);
                    Gizmos.DrawCube(center, size);
                }
            }
            else if (CharacterPlacementManager.Instance.IsTileUnderAttack(GridCoordinates))
            {
                // ATTACK HIGHLIGHT (Red)
                // Draw a thick frame-like box for attack zone
                Gizmos.color = new Color(1, 0.1f, 0.1f, 0.9f); // Bright Red
                Vector3 center = transform.position + Vector3.up * 0.05f;
                Vector3 size = new Vector3(0.95f, 0.1f, 0.95f);
                
                Gizmos.DrawWireCube(center, size);
                
                // Add a second wire cube slightly offset for "thickness"
                Gizmos.DrawWireCube(center, size * 0.98f);

                // Semi-transparent Danger Fill
                Gizmos.color = new Color(1, 0, 0, 0.25f);
                Gizmos.DrawCube(center, size);
            }
            else if (CharacterPlacementManager.Instance.IsTilePotentiallyUnderAttack(GridCoordinates))
            {
                // POTENTIAL ATTACK HIGHLIGHT (Pale Red - All possible variants)
                Gizmos.color = new Color(1, 0.1f, 0.1f, 0.3f); // Pale Red Wireframe
                Vector3 center = transform.position + Vector3.up * 0.05f;
                Vector3 size = new Vector3(0.95f, 0.1f, 0.95f);
                
                Gizmos.DrawWireCube(center, size);

                // Very light fill
                Gizmos.color = new Color(1, 0, 0, 0.05f);
                Gizmos.DrawCube(center, size);
            }
        }
    }
}