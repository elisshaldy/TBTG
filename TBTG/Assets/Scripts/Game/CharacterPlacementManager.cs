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
    // Key is (OwnerID, CardID), Value is LibraryIndex
    private Dictionary<(int, int), int> _spawnedCharLibIndices = new Dictionary<(int, int), int>();
    // Key is GridCoordinates, Value is (OwnerID, CardID)
    private Dictionary<Vector2Int, (int, int)> _tileOccupants = new Dictionary<Vector2Int, (int, int)>();

    private int _localPlayerIndex = -1;

    // Preview logic
    private GameObject _localPreviewInstance;
    private Dictionary<int, GameObject> _remotePreviewInstances = new Dictionary<int, GameObject>();
    
    private CardDragHandler _currentPreviewCard;
    private Tile _currentPreviewTile;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        // Auto-find library if not assigned
        if (_library == null)
        {
            var initializer = FindObjectOfType<GameDataInitializer>();
            if (initializer != null)
            {
                // Using reflection or just finding the asset is hard, 
                // but we can try to find it on the initializer if it was public.
                // For now, we assume user assigns it or we find it via Resources if possible.
            }
        }
    }

    private void Start()
    {
        if (photonView == null)
        {
            Debug.LogError("[CharacterPlacementManager] MISSING PhotonView! Please add PhotonView component to this GameObject for multiplayer to work.");
        }

        _localPlayerIndex = PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber - 1 : 0;

        if (PhotonNetwork.InRoom && photonView != null)
        {
            Debug.Log("[Placement] Requesting current placements from others...");
            photonView.RPC("RPC_RequestCurrentPlacements", RpcTarget.Others);
        }
    }

    #region PLACEMENT LOGIC

    public bool TryPlaceCharacter(CardDragHandler card, Tile tile)
    {
        if (tile == null || tile.Type == TileType.Impassable) return false;

        // Check if tile is occupied by another character
        if (_tileOccupants.TryGetValue(tile.GridCoordinates, out var occupant))
        {
            if (occupant.Item1 != card.OwnerID || occupant.Item2 != card.CardID)
            {
                Debug.Log("[Placement] Tile occupied!");
                return false;
            }
        }

        var cardInfo = card.GetComponent<CardInfo>();
        if (cardInfo == null || cardInfo.CharData == null) return false;

        int charLibraryIndex = GetLibraryIndex(cardInfo.CharData);
        int ownerID = card.OwnerID;

        // Perform local placement
        PerformPlacement(ownerID, card.CardID, charLibraryIndex, tile.GridCoordinates, tile);

        // Sync with others
        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC("RPC_PlaceCharacter", RpcTarget.Others, ownerID, card.CardID, charLibraryIndex, tile.GridCoordinates.x, tile.GridCoordinates.y);
        }

        return true;
    }

    [PunRPC]
    private void RPC_PlaceCharacter(int ownerID, int cardID, int libIdx, int tx, int ty)
    {
        Debug.Log($"[Placement] Received RPC from Player {ownerID} for Card {cardID} at ({tx}, {ty})");
        Vector2Int targetPos = new Vector2Int(tx, ty);
        Tile targetTile = FindTileAt(targetPos);
        PerformPlacement(ownerID, cardID, libIdx, targetPos, targetTile);
    }

    private void PerformPlacement(int ownerID, int cardID, int libIdx, Vector2Int gridPos, Tile tile)
    {
        if (tile == null) return;
        var key = (ownerID, cardID);

        ClearPlacementInternal(ownerID, cardID);

        if (_library != null && libIdx >= 0 && libIdx < _library.AllCharacters.Count)
        {
            CharacterData data = _library.AllCharacters[libIdx];
            if (data.CharacterModel != null)
            {
                GameObject characterInstance = Instantiate(data.CharacterModel, tile.transform.position, Quaternion.Euler(-90, 0, 0));
                FitToTile(characterInstance, tile);
                
                var iconWorld = characterInstance.GetComponentInChildren<PlayerIconWorld>();
                if (iconWorld != null) iconWorld.SetIcon(data.CharacterSprite);

                _spawnedCharacters[key] = characterInstance;
                _spawnedCharLibIndices[key] = libIdx;
                _tileOccupants[gridPos] = key;

                ApplyTeamColor(characterInstance, ownerID);
            }
        }
    }

    public void ClearPlacement(CardDragHandler card)
    {
        ClearPlacementInternal(card.OwnerID, card.CardID);
        if (PhotonNetwork.InRoom && photonView != null)
            photonView.RPC("RPC_ClearPlacement", RpcTarget.Others, card.OwnerID, card.CardID);
    }

    [PunRPC]
    private void RPC_ClearPlacement(int ownerID, int cardID) => ClearPlacementInternal(ownerID, cardID);

    private void ClearPlacementInternal(int ownerID, int cardID)
    {
        var key = (ownerID, cardID);
        if (_spawnedCharacters.TryGetValue(key, out GameObject oldChar))
        {
            Destroy(oldChar);
            _spawnedCharacters.Remove(key);
            _spawnedCharLibIndices.Remove(key);
            
            var keysToRemove = _tileOccupants.Where(kvp => kvp.Value == key).Select(kvp => kvp.Key).ToList();
            foreach (var k in keysToRemove) _tileOccupants.Remove(k);
        }
    }

    #endregion

    #region SYNCHRONIZATION FOR LATE JOINERS

    [PunRPC]
    private void RPC_RequestCurrentPlacements(PhotonMessageInfo info)
    {
        Debug.Log($"[Placement] Player {info.Sender.ActorNumber} requested current placements. Sending mine...");
        foreach (var kvp in _spawnedCharLibIndices)
        {
            var key = kvp.Key; // (ownerID, cardID)
            int libIdx = kvp.Value;

            // Only send characters that WE (this client) are responsible for
            // (Assuming ownerID matches local index)
            if (key.Item1 == _localPlayerIndex)
            {
                // Find grid pos for this character
                var gridPosEntry = _tileOccupants.FirstOrDefault(x => x.Value == key);
                if (gridPosEntry.Value == key)
                {
                    photonView.RPC("RPC_PlaceCharacter", info.Sender, key.Item1, key.Item2, libIdx, gridPosEntry.Key.x, gridPosEntry.Key.y);
                }
            }
        }
    }

    #endregion

    #region PREVIEW / HOLOGRAM LOGIC (Networked)

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

        int libIdx = GetLibraryIndex(cardInfo.CharData);
        
        // Local preview
        _localPreviewInstance = SpawnPreviewInstance(libIdx, tile, _localPlayerIndex);

        // Notify others
        if (PhotonNetwork.InRoom && photonView != null)
        {
            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
            photonView.RPC("RPC_ShowRemotePreview", RpcTarget.Others, myActor, libIdx, tile.GridCoordinates.x, tile.GridCoordinates.y);
        }
    }

    public void HidePreview()
    {
        if (_localPreviewInstance != null)
        {
            Destroy(_localPreviewInstance);
            _localPreviewInstance = null;
        }
        _currentPreviewCard = null;
        _currentPreviewTile = null;

        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC("RPC_HideRemotePreview", RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    [PunRPC]
    private void RPC_ShowRemotePreview(int actorNum, int libIdx, int tx, int ty)
    {
        if (_remotePreviewInstances.TryGetValue(actorNum, out GameObject existing))
        {
            Destroy(existing);
        }

        Tile tile = FindTileAt(new Vector2Int(tx, ty));
        if (tile != null)
        {
            GameObject preview = SpawnPreviewInstance(libIdx, tile, actorNum - 1);
            _remotePreviewInstances[actorNum] = preview;
        }
    }

    [PunRPC]
    private void RPC_HideRemotePreview(int actorNum)
    {
        if (_remotePreviewInstances.TryGetValue(actorNum, out GameObject preview))
        {
            Destroy(preview);
            _remotePreviewInstances.Remove(actorNum);
        }
    }

    private GameObject SpawnPreviewInstance(int libIdx, Tile tile, int ownerID)
    {
        if (_library == null || libIdx < 0 || libIdx >= _library.AllCharacters.Count) return null;

        CharacterData data = _library.AllCharacters[libIdx];
        if (data == null || data.CharacterModel == null) return null;

        GameObject preview = Instantiate(data.CharacterModel, tile.transform.position, Quaternion.Euler(-90, 0, 0));
        FitToTile(preview, tile);
        
        Color pColor = GetColorForPlayer(ownerID);
        ApplyHologramEffect(preview, pColor);

        var iconWorld = preview.GetComponentInChildren<PlayerIconWorld>();
        if (iconWorld != null) iconWorld.SetIcon(data.CharacterSprite);

        return preview;
    }

    #endregion

    #region HELPERS

    private int GetLibraryIndex(CharacterData data) => _library != null ? _library.AllCharacters.IndexOf(data) : -1;

    private Tile FindTileAt(Vector2Int pos)
    {
        if (GridManager.Instance != null) return GridManager.Instance.GetTile(pos);
        return FindObjectsOfType<Tile>().FirstOrDefault(t => t.GridCoordinates == pos);
    }

    private Color GetColorForPlayer(int ownerID)
    {
        // Player 1 (index 0) = Green, Player 2 (index 1) = Red. 
        // Others rotate or default to red.
        return ownerID == 0 ? Color.green : Color.red;
    }

    private void ApplyTeamColor(GameObject obj, int ownerID)
    {
        Color teamColor = GetColorForPlayer(ownerID);
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty("_Color")) mat.color = teamColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", teamColor);
            }
        }
    }

    private void ApplyHologramEffect(GameObject obj, Color color)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                mat.SetFloat("_Mode", 2); 
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                
                Color col = color; 
                col.a = 0.4f;
                
                if (mat.HasProperty("_Color")) mat.color = col;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
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
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        float maxCharSize = Mathf.Max(bounds.size.x, bounds.size.z);
        if (maxCharSize > 0)
        {
            float scaleFactor = (tileSize * 0.8f) / maxCharSize;
            character.transform.localScale *= scaleFactor;
        }
    }
    #endregion
}
