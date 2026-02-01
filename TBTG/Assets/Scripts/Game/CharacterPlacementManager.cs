using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

public class CharacterPlacementManager : MonoBehaviourPunCallbacks
{
    public static CharacterPlacementManager Instance { get; private set; }

    [SerializeField] private GameDataLibrary _library;

    // Key is (OwnerID, CardID)
    private Dictionary<(int, int), GameObject> _spawnedCharacters = new Dictionary<(int, int), GameObject>();
    // Key is GridCoordinates, Value is (OwnerID, CardID)
    private Dictionary<Vector2Int, (int, int)> _tileOccupants = new Dictionary<Vector2Int, (int, int)>();

    private GameObject _previewInstance;
    private CardDragHandler _currentPreviewCard;
    private Tile _currentPreviewTile;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public bool TryPlaceCharacter(CardDragHandler card, Tile tile)
    {
        if (tile == null || tile.Type == TileType.Impassable) 
        {
            Debug.Log("[Placement] Cannot place on null or impassable tile.");
            return false;
        }

        // Check if tile is occupied by another character
        if (_tileOccupants.TryGetValue(tile.GridCoordinates, out var occupant))
        {
            if (occupant.Item1 != card.OwnerID || occupant.Item2 != card.CardID)
            {
                Debug.Log("[Placement] Tile already occupied by another character!");
                return false;
            }
        }

        var cardInfo = card.GetComponent<CardInfo>();
        if (cardInfo == null || cardInfo.CharData == null) return false;

        int charLibraryIndex = _library != null ? _library.AllCharacters.IndexOf(cardInfo.CharData) : -1;
        int ownerID = card.OwnerID;

        // Perform local placement using the tile we already have
        PerformPlacement(ownerID, card.CardID, charLibraryIndex, tile.GridCoordinates, tile);

        // Sync with others if in multiplayer
        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC("RPC_PlaceCharacter", RpcTarget.Others, ownerID, card.CardID, charLibraryIndex, tile.GridCoordinates.x, tile.GridCoordinates.y);
        }

        return true;
    }

    [PunRPC]
    private void RPC_PlaceCharacter(int ownerID, int cardID, int libIdx, int tx, int ty)
    {
        Vector2Int targetPos = new Vector2Int(tx, ty);
        Tile targetTile = null;

        // Fallback: If GridManager is missing, find tile manually by coordinates
        if (GridManager.Instance != null)
        {
            targetTile = GridManager.Instance.GetTile(targetPos);
        }
        else
        {
            targetTile = FindObjectsOfType<Tile>().FirstOrDefault(t => t.GridCoordinates == targetPos);
        }

        PerformPlacement(ownerID, cardID, libIdx, targetPos, targetTile);
    }

    private void PerformPlacement(int ownerID, int cardID, int libIdx, Vector2Int gridPos, Tile tile)
    {
        if (tile == null) return;
        var key = (ownerID, cardID);

        // Clear this character's previous position if any (but NOT other players' characters)
        ClearPlacementInternal(ownerID, cardID);

        // Spawn/Place new character
        if (_library != null && libIdx >= 0 && libIdx < _library.AllCharacters.Count)
        {
            CharacterData data = _library.AllCharacters[libIdx];
            GameObject prefab = data.CharacterModel;

            if (prefab != null)
            {
                GameObject characterInstance = Instantiate(prefab, tile.transform.position, Quaternion.Euler(-90, 0, 0));
                FitToTile(characterInstance, tile);
                
                // Setup character icon
                var iconWorld = characterInstance.GetComponentInChildren<PlayerIconWorld>();
                if (iconWorld != null) iconWorld.SetIcon(data.CharacterSprite);

                _spawnedCharacters[key] = characterInstance;
                _tileOccupants[gridPos] = key;
                
                Debug.Log($"[Placement] Placed {data.CharacterName} (Owner {ownerID}) on tile {gridPos}");
            }
        }
    }

    public void ClearPlacement(CardDragHandler card)
    {
        ClearPlacementInternal(card.OwnerID, card.CardID);
        
        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC("RPC_ClearPlacement", RpcTarget.Others, card.OwnerID, card.CardID);
        }
    }

    [PunRPC]
    private void RPC_ClearPlacement(int ownerID, int cardID)
    {
        ClearPlacementInternal(ownerID, cardID);
    }

    private void ClearPlacementInternal(int ownerID, int cardID)
    {
        var key = (ownerID, cardID);
        if (_spawnedCharacters.TryGetValue(key, out GameObject oldChar))
        {
            Destroy(oldChar);
            _spawnedCharacters.Remove(key);
            
            // Using a temporary list to avoid modification while enumerating
            List<Vector2Int> keysToRemove = new List<Vector2Int>();
            foreach (var kvp in _tileOccupants)
            {
                if (kvp.Value == key)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var k in keysToRemove) _tileOccupants.Remove(k);
        }
    }

    public void ShowPreview(CardDragHandler card, Tile tile)
    {
        if (tile == null || tile.Type == TileType.Impassable)
        {
            HidePreview();
            return;
        }

        if (_currentPreviewCard == card && _currentPreviewTile == tile) return;

        HidePreview();

        _currentPreviewCard = card;
        _currentPreviewTile = tile;

        var cardInfo = card.GetComponent<CardInfo>();
        if (cardInfo == null || cardInfo.CharData == null) return;

        GameObject prefab = cardInfo.CharData.CharacterModel;
        if (prefab != null)
        {
            _previewInstance = Instantiate(prefab, tile.transform.position, Quaternion.Euler(-90, 0, 0));
            FitToTile(_previewInstance, tile);
            ApplyHologramEffect(_previewInstance);

            // Setup character icon for preview
            var iconWorld = _previewInstance.GetComponentInChildren<PlayerIconWorld>();
            if (iconWorld != null) iconWorld.SetIcon(cardInfo.CharData.CharacterSprite);
        }
    }

    public void HidePreview()
    {
        if (_previewInstance != null)
        {
            Destroy(_previewInstance);
            _previewInstance = null;
        }
        _currentPreviewCard = null;
        _currentPreviewTile = null;
    }

    private void ApplyHologramEffect(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                // Set to Fade/Transparent mode (Standard Shader hack)
                mat.SetFloat("_Mode", 2); 
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                Color hologramColor = Color.green;
                hologramColor.a = 0.4f;
                if (mat.HasProperty("_Color")) mat.color = hologramColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hologramColor);
            }
        }
    }

    private void FitToTile(GameObject character, Tile tile)
    {
        float tileSize = 1.0f;
        var generator = FindObjectOfType<MapGenerator>();
        if (generator != null) tileSize = generator.TileSize;

        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        float charWidth = bounds.size.x;
        float charDepth = bounds.size.z;
        float maxCharSize = Mathf.Max(charWidth, charDepth);

        if (maxCharSize > 0)
        {
            float targetSize = tileSize * 0.8f;
            float scaleFactor = targetSize / maxCharSize;
            character.transform.localScale *= scaleFactor;
        }
    }
}
