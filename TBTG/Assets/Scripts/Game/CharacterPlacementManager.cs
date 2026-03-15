using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun;
using System.Linq;

public class CharacterPlacementManager : MonoBehaviourPunCallbacks
{
    public static CharacterPlacementManager Instance { get; private set; }

    [SerializeField] private GameDataLibrary _library;
    [SerializeField] private CardDeckController _deckController;

    // Key is (OwnerID, PairID)
    private Dictionary<(int, int), GameObject> _spawnedCharacters = new Dictionary<(int, int), GameObject>();
    // Key is (OwnerID, PairID), Value is LibraryIndex of the CURRENTLY ACTIVE card
    private Dictionary<(int, int), int> _spawnedCharLibIndices = new Dictionary<(int, int), int>();
    // Key is (OwnerID, PairID), Value is list of applied mod indices (from library)
    private Dictionary<(int, int), int[]> _spawnedCharModIndices = new Dictionary<(int, int), int[]>();
    // Key is GridCoordinates, Value is (OwnerID, PairID)
    private Dictionary<Vector2Int, (int, int)> _tileOccupants = new Dictionary<Vector2Int, (int, int)>();
    
    // Grid Occupants
    [SerializeField] private GameObject _cardPrefabForUI; // Prefab with CardInfo
    private GameObject _bigCardModal; // Overlay panel
    private GameObject _activeBigCard;
    private (int, int) _activeBigCardKey = (-1, -1);
    private HashSet<(int, int)> _revealedEnemyMods = new HashSet<(int, int)>();

    private (int ownerID, int pairID) _activeCharacterKey = (-1, -1);
    private CharacterData _activeCharacterData;
    private int _activeOrientation = 0; // 0=0, 1=90, 2=180, 3=270 deg
    private bool _isReversed = false;

    private MovementCard _activeMovementCard = null;
    private MovementCardInfo _activeMovementCardInfo = null;
    public bool IsMovementModeActive => _activeMovementCard != null;
    public MovementCard ActiveMovementCard => _activeMovementCard;

    private int _localPlayerIndex = -1;
    private GameSceneState _gameSceneState;

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
        }

        if (_deckController == null)
        {
            _deckController = FindObjectOfType<CardDeckController>();
        }
        
        // Auto-fix for null references to avoid broken placements
        if (_library == null) _library = FindObjectOfType<GameDataLibrary>();
        if (_deckController == null) _deckController = FindObjectOfType<CardDeckController>();
        if (_gameSceneState == null) _gameSceneState = FindObjectOfType<GameSceneState>();
    }

    private void Update()
    {
        // Handle rotation during active turn
        if (InitiativeSystem.Instance != null && InitiativeSystem.Instance.IsFinalized)
        {
            if (_activeCharacterData != null)
            {
                bool changed = false;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    _activeOrientation = (_activeOrientation + 1) % 4;
                    changed = true;
                }
                else if (Input.GetKeyDown(KeyCode.Q))
                {
                    _activeOrientation = (_activeOrientation + 3) % 4;
                    changed = true;
                }
                else if (Input.GetKeyDown(KeyCode.R))
                {
                    _isReversed = !_isReversed;
                    changed = true;
                }

                if (changed)
                {
                    UpdateVisualRotation();
                }
            }
        }
    }

    private void UpdateVisualRotation(bool sendRPC = true)
    {
        GameObject charObj = GetCharacterObject(_activeCharacterKey.ownerID, _activeCharacterKey.pairID);
        if (charObj != null)
        {
            // Rotate model around Y axis. Original models might have -90 X rotation.
            charObj.transform.rotation = Quaternion.Euler(-90, _activeOrientation * 90f, 0);

            // Apply Mirror effect on X axis
            Vector3 currentScale = charObj.transform.localScale;
            currentScale.x = Mathf.Abs(currentScale.x) * (_isReversed ? -1f : 1f);
            charObj.transform.localScale = currentScale;
        }

        if (sendRPC && PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC("RPC_SyncRotation", RpcTarget.Others, _activeCharacterKey.ownerID, _activeCharacterKey.pairID, _activeOrientation, _isReversed);
        }
    }

    [PunRPC]
    private void RPC_SyncRotation(int ownerID, int pairID, int orientation, bool isReversed)
    {
        GameObject charObj = GetCharacterObject(ownerID, pairID);
        if (charObj != null)
        {
            charObj.transform.rotation = Quaternion.Euler(-90, orientation * 90f, 0);
            Vector3 currentScale = charObj.transform.localScale;
            currentScale.x = Mathf.Abs(currentScale.x) * (isReversed ? -1f : 1f);
            charObj.transform.localScale = currentScale;
        }
    }

    private IEnumerator Start()
    {
        if (photonView == null)
        {
            Debug.LogError("[CharacterPlacementManager] MISSING PhotonView! Please add PhotonView component to this GameObject for multiplayer to work.");
        }

        _localPlayerIndex = PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber : 1;
        
        Debug.Log($"[Placement] Manager started. LocalPlayerIndex: {_localPlayerIndex}. Waiting for grid...");

        // Wait for GridManager and tiles to be ready
        float timeout = 5f;
        while (timeout > 0)
        {
            if (GridManager.Instance != null && GridManager.Instance.GetAllRegisteredCoords().Any())
                break;
            
            yield return null;
            timeout -= Time.deltaTime;
        }

        // Extra small delay to ensure all tiles are fully registered and GridManager is populated
        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.InRoom && photonView != null)
        {
            Debug.Log($"[Placement] Player {_localPlayerIndex} requesting current placements from others...");
            photonView.RPC("RPC_RequestCurrentPlacements", RpcTarget.Others);
        }
    }

    #region PLACEMENT LOGIC

    public bool TryPlaceCharacter(CardDragHandler card, Tile tile)
    {
        if (tile == null || tile.Type == TileType.Impassable) return false;
        
        // NEW: Cannot place after round started
        if (InitiativeSystem.Instance != null && InitiativeSystem.Instance.IsFinalized)
        {
            Debug.Log("[Placement] Cannot place characters - round already started!");
            return false;
        }

        // NEW: Check placement zone ownership
        if (tile.PlacementOwnerID != card.OwnerID)
        {
            Debug.Log($"[Placement] Cannot place here! Tile belongs to Player {tile.PlacementOwnerID}, but Card belongs to Player {card.OwnerID}");
            return false;
        }

        // Check if tile is occupied by another character
        if (_tileOccupants.TryGetValue(tile.GridCoordinates, out var occupant))
        {
            if (occupant.Item1 != card.OwnerID || occupant.Item2 != card.PairID)
            {
                Debug.Log("[Placement] Tile occupied!");
                return false;
            }
        }

        var cardInfo = card.GetComponent<CardInfo>();
        if (cardInfo == null || cardInfo.CharData == null) return false;

        int charLibraryIndex = GetLibraryIndex(cardInfo.CharData);
        int ownerID = card.OwnerID;

        // Collect mod indices
        List<int> modIndices = new List<int>();
        var modsContainer = card.GetComponent<ModsCardContainer>();
        if (modsContainer != null)
        {
            foreach (var mod in modsContainer._mods)
            {
                if (mod != null) modIndices.Add(_library.AllMods.IndexOf(mod));
            }
        }
        int[] modIdxArray = modIndices.ToArray();

        // Perform local placement
        PerformPlacement(ownerID, card.PairID, charLibraryIndex, modIdxArray, tile.GridCoordinates, tile);

        // Sync with others
        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC("RPC_PlaceCharacter", RpcTarget.OthersBuffered, ownerID, card.PairID, charLibraryIndex, modIdxArray, tile.GridCoordinates.x, tile.GridCoordinates.y);
        }

        return true;
    }

    [PunRPC]
    private void RPC_PlaceCharacter(int ownerID, int pairID, int libIdx, int[] modIndices, int tx, int ty)
    {
        Debug.Log($"[Placement] Received RPC for Player {ownerID}, Pair {pairID} at ({tx}, {ty})");
        Vector2Int targetPos = new Vector2Int(tx, ty);
        
        // Use a coroutine to handle cases where the grid might not be ready yet
        StartCoroutine(WaitAndPlace(ownerID, pairID, libIdx, modIndices, targetPos));
    }

    private IEnumerator WaitAndPlace(int ownerID, int pairID, int libIdx, int[] modIndices, Vector2Int gridPos)
    {
        Tile targetTile = null;
        float timeout = 10f; // Increased timeout for slow map generation or stage transitions
        
        while (targetTile == null && timeout > 0)
        {
            targetTile = FindTileAt(gridPos);
            if (targetTile == null)
            {
                yield return new WaitForSeconds(0.2f);
                timeout -= 0.2f;
            }
        }

        if (targetTile != null)
        {
            PerformPlacement(ownerID, pairID, libIdx, modIndices, gridPos, targetTile);
        }
        else
        {
            Debug.LogError($"[Placement] CRITICAL: Could not find tile at {gridPos} for character placement (Owner:{ownerID}, Pair:{pairID})!");
        }
    }

    private void PerformPlacement(int ownerID, int pairID, int libIdx, int[] modIndices, Vector2Int gridPos, Tile tile)
    {
        if (tile == null) return;
        var key = (ownerID, pairID);

        ClearPlacementInternal(ownerID, pairID);

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
                _spawnedCharModIndices[key] = modIndices;
                _tileOccupants[gridPos] = key;

                // Add click handler
                var clickHandler = characterInstance.AddComponent<CharacterWorldClickHandler>();
                clickHandler.Initialize(ownerID, pairID, data);
                clickHandler.OnCharacterHidden += HandleCharacterHidden;

                ApplyTeamColor(characterInstance, ownerID);
            }
        }

        if (InitiativeSystem.Instance != null)
        {
            InitiativeSystem.Instance.UpdateAcceptButton();
        }
    }

    private void HandleCharacterHidden(int ownerID, int pairID)
    {
        if (_activeBigCardKey == (ownerID, pairID))
        {
            HideBigCard();
        }
    }

    public void ClearPlacement(CardDragHandler card)
    {
        ClearPlacementInternal(card.OwnerID, card.PairID);
        if (PhotonNetwork.InRoom && photonView != null)
            photonView.RPC("RPC_ClearPlacement", RpcTarget.OthersBuffered, card.OwnerID, card.PairID);
    }

    [PunRPC]
    private void RPC_ClearPlacement(int ownerID, int pairID) => ClearPlacementInternal(ownerID, pairID);

    public bool IsPairPlaced(int ownerID, int pairID)
    {
        return _spawnedCharacters.ContainsKey((ownerID, pairID));
    }

    public void SetCharacterActive(int ownerID, int pairID, bool isActive)
    {
        var key = (ownerID, pairID);
        GameObject charObj = null;

        // 1. Try direct lookup
        if (!_spawnedCharacters.TryGetValue(key, out charObj))
        {
            // 2. Fallback for Hotseat/Debug: search by PairID among all spawned chars
            foreach (var kvp in _spawnedCharacters)
            {
                if (kvp.Key.Item2 == pairID && (ownerID <= 0 || kvp.Key.Item1 == ownerID))
                {
                    charObj = kvp.Value;
                    break;
                }
            }
        }

        if (charObj != null)
        {
            var visual = charObj.GetComponent<CharacterVisual>();
            if (visual == null) visual = charObj.GetComponentInChildren<CharacterVisual>();
            
            if (visual != null)
            {
                visual.SetIsActive(isActive);
            }

            if (isActive)
            {
                _activeCharacterKey = key;
                _activeCharacterData = GetCharacterData(ownerID, pairID);
                _activeOrientation = 0; // Reset orientation for new active character
                _isReversed = false;    // Reset mirror for new active character
                UpdateVisualRotation();
            }
            else if (_activeCharacterKey == key)
            {
                _activeCharacterKey = (-1, -1);
                _activeCharacterData = null;
                _isReversed = false;
            }
        }
    }

    public int GetSpawnedCharacterLibIndex(int ownerID, int pairID)
    {
        if (_spawnedCharLibIndices.TryGetValue((ownerID, pairID), out int libIdx))
        {
            return libIdx;
        }
        return -1;
    }

    private CharacterData GetCharacterData(int ownerID, int pairID)
    {
        int libIdx = GetSpawnedCharacterLibIndex(ownerID, pairID);
        if (_library != null && libIdx >= 0 && libIdx < _library.AllCharacters.Count)
            return _library.AllCharacters[libIdx];
        
        return null;
    }

    public bool IsTileUnderAttack(Vector2Int tileCoords)
    {
        if (_activeCharacterKey == (-1, -1)) return false;
        return CheckPositionInAttackPattern(_activeCharacterKey.ownerID, _activeCharacterKey.pairID, tileCoords, _activeOrientation, _isReversed);
    }

    private bool CheckPositionInAttackPattern(int attackerOwnerID, int attackerPairID, Vector2Int targetPos, int orientation, bool isReversed)
    {
        CharacterData data = GetCharacterData(attackerOwnerID, attackerPairID);
        if (data == null || data.AttackPatternGrid == null) return false;

        var attackerKey = (attackerOwnerID, attackerPairID);
        var gridSlot = _tileOccupants.FirstOrDefault(x => x.Value == attackerKey);
        if (gridSlot.Value != attackerKey) return false;

        Vector2Int charGridPos = gridSlot.Key;
        Vector2Int relOffset = targetPos - charGridPos;
        
        // World-to-Pattern rotation
        Vector2Int rotatedRel = relOffset;
        switch (orientation)
        {
            case 1: rotatedRel = new Vector2Int(relOffset.y, -relOffset.x); break; 
            case 2: rotatedRel = new Vector2Int(-relOffset.x, -relOffset.y); break; 
            case 3: rotatedRel = new Vector2Int(-relOffset.y, relOffset.x); break; 
        }

        if (isReversed) rotatedRel.x = -rotatedRel.x;

        Vector2Int patternCharPos = data.AttackPatternGrid.CharacterPosition;
        int px = rotatedRel.x + patternCharPos.x;
        int py = rotatedRel.y + patternCharPos.y;

        if (px >= 0 && px < 3 && py >= 0 && py < 3)
        {
            return data.AttackPatternGrid.Get(px, py);
        }

        return false;
    }

    public bool IsTilePotentiallyUnderAttack(Vector2Int tileCoords)
    {
        if (_activeCharacterData == null || _activeCharacterData.AttackPatternGrid == null) return false;

        var gridSlot = _tileOccupants.FirstOrDefault(x => x.Value == _activeCharacterKey);
        if (gridSlot.Value != _activeCharacterKey) return false;

        Vector2Int charGridPos = gridSlot.Key;
        Vector2Int relOffset = tileCoords - charGridPos;
        Vector2Int patternCharPos = _activeCharacterData.AttackPatternGrid.CharacterPosition;

        for (int flip = 0; flip < 2; flip++)
        {
            for (int rot = 0; rot < 4; rot++)
            {
                Vector2Int currentRel = relOffset;
                
                switch (rot)
                {
                    case 1: currentRel = new Vector2Int(currentRel.y, -currentRel.x); break; 
                    case 2: currentRel = new Vector2Int(-currentRel.x, -currentRel.y); break; 
                    case 3: currentRel = new Vector2Int(-currentRel.y, currentRel.x); break; 
                }

                if (flip == 1)
                {
                    currentRel.x = -currentRel.x;
                }

                int px = currentRel.x + patternCharPos.x;
                int py = currentRel.y + patternCharPos.y;

                if (px >= 0 && px < 3 && py >= 0 && py < 3 && _activeCharacterData.AttackPatternGrid.Get(px, py))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsTileMovable(Vector2Int tileCoords)
    {
        if (_activeMovementCard == null || _activeMovementCard.MovementPatternGrid == null) return false;

        var gridSlot = _tileOccupants.FirstOrDefault(x => x.Value == _activeCharacterKey);
        if (gridSlot.Value != _activeCharacterKey) return false;

        Vector2Int charGridPos = gridSlot.Key;
        Vector2Int relOffset = tileCoords - charGridPos;
        Vector2Int patternCharPos = _activeMovementCard.MovementPatternGrid.CharacterPosition;

        // Перевіряємо всі 8 варіантів (4 повороти + 2 віддзеркалення),
        // щоб одразу відобразити всі можливі напрямки на карті навколо юніта.
        for (int flip = 0; flip < 2; flip++)
        {
            for (int rot = 0; rot < 4; rot++)
            {
                Vector2Int currentRel = relOffset;
                
                // Приміняємо поворот
                switch (rot)
                {
                    case 1: currentRel = new Vector2Int(currentRel.y, -currentRel.x); break; 
                    case 2: currentRel = new Vector2Int(-currentRel.x, -currentRel.y); break; 
                    case 3: currentRel = new Vector2Int(-currentRel.y, currentRel.x); break; 
                }

                // Приміняємо віддзеркалення
                if (flip == 1)
                {
                    currentRel.x = -currentRel.x;
                }

                int px = currentRel.x + patternCharPos.x;
                int py = currentRel.y + patternCharPos.y;

                if (px >= 0 && px < 3 && py >= 0 && py < 3)
                {
                    if (_activeMovementCard.MovementPatternGrid.Get(px, py))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void SetMovementMode(MovementCardInfo cardInfo)
    {
        if (cardInfo != null)
        {
            // SECURE OWNERSHIP: Only allow if card owner matches active unit owner
            if (cardInfo.OwnerID != _activeCharacterKey.ownerID)
            {
                Debug.LogWarning($"[Movement] Ownership mismatch! Card belongs to P{cardInfo.OwnerID}, Unit belongs to P{_activeCharacterKey.ownerID}");
                return;
            }
        }
        _activeMovementCardInfo = cardInfo;
        _activeMovementCard = cardInfo != null ? cardInfo.MoveCard : null;
    }

    public void ClearMovementMode()
    {
        _activeMovementCard = null;
        _activeMovementCardInfo = null;
    }

    /// <summary>
    /// Спроба перемістити активного персонажа на вказану клітинку
    /// </summary>
    public void TryMoveActiveCharacter(Vector2Int targetPos)
    {
        if (!IsMovementModeActive) return;
        if (_activeCharacterKey == (-1, -1)) return;

        // 1. ПЕРЕВІРКА ПАТЕРНУ
        if (!IsTileMovable(targetPos))
        {
            Debug.Log("[Movement] Target tile not in pattern!");
            return;
        }

        // 2. ПЕРЕВІРКА КОЛІЗІЇ (чи зайнята клітинка?)
        if (_tileOccupants.ContainsKey(targetPos))
        {
            Debug.Log("[Movement] Target tile is already occupied!");
            return;
        }

        // 3. ВИКОНАННЯ ХОДУ
        string cardName = _activeMovementCard != null ? _activeMovementCard.name : "";
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_MoveCharacter", RpcTarget.All, _activeCharacterKey.ownerID, _activeCharacterKey.pairID, targetPos.x, targetPos.y, cardName);
        }
        else
        {
            ExecuteMovement(_activeCharacterKey.ownerID, _activeCharacterKey.pairID, targetPos, cardName);
        }
    }

    [PunRPC]
    private void RPC_MoveCharacter(int ownerID, int pairID, int tx, int ty, string cardName)
    {
        ExecuteMovement(ownerID, pairID, new Vector2Int(tx, ty), cardName);
    }

    private void ExecuteMovement(int ownerID, int pairID, Vector2Int targetPos, string cardName)
    {
        var key = (ownerID, pairID);
        var kvp = _tileOccupants.FirstOrDefault(x => x.Value == key);
        if (kvp.Value != key) return;

        Vector2Int oldPos = kvp.Key;
        _tileOccupants.Remove(oldPos);
        _tileOccupants[targetPos] = key;

        GameObject charObj = GetCharacterObject(ownerID, pairID);
        if (charObj != null)
        {
            Tile t = FindTileAt(targetPos);
            if (t != null)
            {
                charObj.transform.position = t.transform.position + Vector3.up * 0.05f;
            }
        }

        // ВИДАЛЕННЯ КАРТКИ
        // Якщо це локальний хід, ми вже маємо посилання. Якщо ні (або RPC), шукаємо по імені та власнику.
        MovementCardInfo targetCard = _activeMovementCardInfo;
        if (targetCard == null && !string.IsNullOrEmpty(cardName))
        {
            targetCard = FindObjectsOfType<MovementCardInfo>()
                .FirstOrDefault(c => c.OwnerID == ownerID && c.MoveCard != null && c.MoveCard.name == cardName);
        }

        if (targetCard != null)
        {
            // PERSISTENCE: Remove from the logical hand in settings
            if (_gameSceneState != null && _gameSceneState._currentSettings != null)
            {
                var snapshot = _gameSceneState._currentSettings.GetSnapshot(ownerID);
                if (snapshot != null && targetCard.MoveCard != null)
                {
                    snapshot.SelectedMovementCards.Remove(targetCard.MoveCard);
                }
            }
            Destroy(targetCard.gameObject);
        }

        ClearMovementMode(); 
        
        // NEW: Turn logic
        if (InitiativeSystem.Instance != null)
        {
            InitiativeSystem.Instance.ConsumeAction();
        }
    }

    /// <summary>
    /// Спроба атакувати клітинку
    /// </summary>
    public void TryAttackTile(Vector2Int targetPos)
    {
        if (IsMovementModeActive) return; // Не атакуємо, якщо вибрана карта ходу
        if (_activeCharacterKey == (-1, -1)) return;

        // 1. ПЕРЕВІРКА ПАТЕРНУ
        if (!IsTileUnderAttack(targetPos))
        {
            Debug.Log("[Combat] Target tile not in attack pattern!");
            return;
        }

        // 2. ВИКОНАННЯ АТАКИ
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_AttackCharacter", RpcTarget.All, _activeCharacterKey.ownerID, _activeCharacterKey.pairID, targetPos.x, targetPos.y, _activeOrientation, _isReversed);
        }
        else
        {
            ExecuteAttack(_activeCharacterKey.ownerID, _activeCharacterKey.pairID, targetPos, _activeOrientation, _isReversed);
        }
    }

    [PunRPC]
    private void RPC_AttackCharacter(int attackerOwnerID, int attackerPairID, int tx, int ty, int orientation, bool isReversed)
    {
        ExecuteAttack(attackerOwnerID, attackerPairID, new Vector2Int(tx, ty), orientation, isReversed);
    }

    private void ExecuteAttack(int attackerOwnerID, int attackerPairID, Vector2Int triggerPos, int orientation, bool isReversed)
    {
        // 1. Ефект на атакуючому та отримання даних
        GameObject attackerObj = GetCharacterObject(attackerOwnerID, attackerPairID);
        if (attackerObj != null && EffectManager.Instance != null)
            EffectManager.Instance.PlayAttackerEffect(attackerObj.transform.position);

        bool hitAny = false;
        
        // Создаем копию списка координат, чтобы избежать ошибок при модификации коллекции (хотя у нас тут только чтение)
        var allOccupants = _tileOccupants.ToList();

        foreach (var occupant in allOccupants)
        {
            if (CheckPositionInAttackPattern(attackerOwnerID, attackerPairID, occupant.Key, orientation, isReversed))
            {
                ApplyDamageToVictim(occupant.Value.Item1, occupant.Value.Item2, occupant.Key);
                hitAny = true;
            }
        }

        // 3. Если патерн вообще никого не зацепил — играем эффект промаха на клетке клика
        if (!hitAny)
        {
            Tile t = FindTileAt(triggerPos);
            if (t != null && EffectManager.Instance != null)
                EffectManager.Instance.PlayMissEffect(t.transform.position);
        }

        // 4. Завершение хода (ПОВНОЕ)
        if (InitiativeSystem.Instance != null && attackerOwnerID == InitiativeSystem.Instance.CurrentTurnPlayerID)
        {
            InitiativeSystem.Instance.ConsumeAction(true);
        }
    }

    private void ApplyDamageToVictim(int victimOwnerID, int victimPairID, Vector2Int pos)
    {
        GameObject victimObj = GetCharacterObject(victimOwnerID, victimPairID);
        CharacterHealthSystem health = null;
        var activeCard = GetActiveCardForUnit(victimOwnerID, victimPairID);
        
        if (activeCard != null)
        {
            health = activeCard.GetComponentInChildren<CharacterHealthSystem>(true);
            if (health == null) health = activeCard.gameObject.AddComponent<CharacterHealthSystem>();
            if (health.OwnerID == -1) health.Initialize(victimOwnerID, victimPairID);
        }

        if (health != null)
        {
            health.TakeHit();
            if (victimObj != null && EffectManager.Instance != null) 
                EffectManager.Instance.PlayHitEffect(victimObj.transform.position);

            if (health.HealthState == CharacterHealthSystem.CharHealth.Dead)
            {
                HandleCharacterDeath(victimOwnerID, victimPairID);
            }
        }
    }

    private void HandleCharacterDeath(int ownerID, int pairID)
    {
        var deadCard = GetActiveCardForUnit(ownerID, pairID);
        if (deadCard == null) return;

        var partnerCard = deadCard.PartnerCard;
        bool isPartnerAlive = false;
        
        if (partnerCard != null)
        {
            var pHealth = partnerCard.GetComponentInChildren<CharacterHealthSystem>();
            if (pHealth != null && pHealth.HealthState > CharacterHealthSystem.CharHealth.Dead)
            {
                isPartnerAlive = true;
            }
        }

        if (isPartnerAlive)
        {
            // Якщо напарник живий — просто перемикаємо на нього модель на полі
            Debug.Log($"[Game] Character in pair {pairID} DIED. Switching to partner.");
            _deckController.MakeActive(partnerCard);
        }
        else
        {
            // Якщо обоє мертві — видаляємо юніта зовсім
            Debug.Log($"[Game] Both characters in pair {pairID} are DEAD. Removing unit from map.");
            
            // 1. Видаляємо з мапи
            ClearPlacementInternal(ownerID, pairID);
            
            // 2. Видаляємо з черги ходів
            if (InitiativeSystem.Instance != null)
            {
                InitiativeSystem.Instance.RemoveFromQueue(ownerID, pairID);
            }

            // 3. Синхронізуємо видалення (RPC_ClearPlacement вже існує в коді)
            if (PhotonNetwork.InRoom)
            {
                photonView.RPC("RPC_ClearPlacement", RpcTarget.Others, ownerID, pairID);
            }
        }

        // Перевіряємо умови завершення гри
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        // Чекаємо, поки ініціатива буде фіналізована (гра почалася)
        if (InitiativeSystem.Instance == null || !InitiativeSystem.Instance.IsFinalized) return;

        // Рахуємо юнітів для обох гравців
        int p1Count = GetPlacedCharacterCount(1);
        int p2Count = GetPlacedCharacterCount(2);

        // Якщо у когось 0 — гра закінчена
        if (p1Count == 0 || p2Count == 0)
        {
            int myID = PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
            
            // Визначаємо результат для локального гравця
            bool isWin = false;
            if (myID == 1) isWin = (p1Count > 0);
            else if (myID == 2) isWin = (p2Count > 0);
            else 
            {
                // Hotseat: хтось виграв, покажемо загальний екран перемоги
                isWin = true; 
            }

            if (isWin)
            {
                string winnerName = "Player";
                if (PhotonNetwork.InRoom)
                {
                    winnerName = PhotonNetwork.LocalPlayer.NickName;
                }
                else if (_gameSceneState != null && _gameSceneState._currentSettings is HotseatSettings hs)
                {
                    winnerName = (p1Count > 0) ? hs.Player1Name : hs.Player2Name;
                }
                
                Debug.Log($"<color=green><b>GRATZ! {winnerName} WINS!</b></color>");

                var screen = FindObjectOfType<WinScreen>(true);
                if (screen != null) screen.Show();
            }
            else
            {
                var screen = FindObjectOfType<LoseScreen>(true);
                if (screen != null) screen.Show();
            }
        }
    }

    public GameObject GetCharacterObject(int ownerID, int pairID)
    {
        var key = (ownerID, pairID);
        if (_spawnedCharacters.TryGetValue(key, out GameObject charObj))
        {
            return charObj;
        }

        foreach (var kvp in _spawnedCharacters)
        {
            if (kvp.Key.Item2 == pairID && (ownerID <= 0 || kvp.Key.Item1 == ownerID))
            {
                return kvp.Value;
            }
        }
        return null;
    }

    public void UpdateCharacterModel(int ownerID, int pairID, int libIdx)
    {
        var key = (ownerID, pairID);

        // Optimization: if the model is already correct, do nothing to avoid flicker
        if (_spawnedCharLibIndices.TryGetValue(key, out int currentIdx) && currentIdx == libIdx)
            return;

        if (_spawnedCharacters.TryGetValue(key, out GameObject charInstance))
        {
            // Find current tile
            var gridSlot = _tileOccupants.FirstOrDefault(x => x.Value == key);
            if (gridSlot.Value == key)
            {
                Tile tile = FindTileAt(gridSlot.Key);
                // Simple approach: re-place at same tile
                int[] mods = _spawnedCharModIndices.TryGetValue(key, out var m) ? m : new int[0];
                PerformPlacement(ownerID, pairID, libIdx, mods, gridSlot.Key, tile);
                
                // Sync
                if (PhotonNetwork.InRoom && photonView != null)
                    photonView.RPC("RPC_UpdateCharacterModel", RpcTarget.OthersBuffered, ownerID, pairID, libIdx, mods);
            }
        }
    }

    [PunRPC]
    private void RPC_UpdateCharacterModel(int ownerID, int pairID, int libIdx, int[] modIndices)
    {
        var key = (ownerID, pairID);
        var gridSlot = _tileOccupants.FirstOrDefault(x => x.Value == key);
        if (gridSlot.Value == key)
        {
            Tile tile = FindTileAt(gridSlot.Key);
            PerformPlacement(ownerID, pairID, libIdx, modIndices, gridSlot.Key, tile);
        }
    }

    private void ClearPlacementInternal(int ownerID, int pairID)
    {
        var key = (ownerID, pairID);
        
        if (_spawnedCharacters.TryGetValue(key, out GameObject oldChar))
        {
            Destroy(oldChar);
            _spawnedCharacters.Remove(key);
        }

        // Always clear internal state even if the model was missing
        _spawnedCharLibIndices.Remove(key);
        _spawnedCharModIndices.Remove(key);
        
        var keysToRemove = _tileOccupants.Where(kvp => kvp.Value == key).Select(kvp => kvp.Key).ToList();
        foreach (var k in keysToRemove) _tileOccupants.Remove(k);

        if (InitiativeSystem.Instance != null)
        {
            InitiativeSystem.Instance.UpdateAcceptButton();
        }
    }

    public int GetPlacedCharacterCount(int ownerID)
    {
        return _spawnedCharacters.Count(kv => kv.Key.Item1 == ownerID);
    }

    #region BIG CARD UI
    public void ToggleBigCard(int ownerID, int pairID, CharacterData data)
    {
        var key = (ownerID, pairID);
        if (_activeBigCard != null && _activeBigCardKey == key)
        {
            HideBigCard();
            return;
        }

        HideBigCard();

        // Find character and icon for positioning
        if (!_spawnedCharacters.TryGetValue(key, out GameObject charObj)) return;

        var iconWorld = charObj.GetComponentInChildren<PlayerIconWorld>();
        float spawnY = charObj.transform.position.y + 3.0f; // Set exactly to 3.0
        
        if (iconWorld != null)
        {
            var iconImg = iconWorld.GetComponentInChildren<Image>();
            spawnY = (iconImg != null) ? iconImg.transform.position.y : iconWorld.transform.position.y;
            
            // Adjust to hit the 3.0 sweet spot
            spawnY += 0.5f;

            // Min height check for consistency
            if (spawnY < charObj.transform.position.y + 1.8f)
                spawnY = charObj.transform.position.y + 3.0f;

            iconWorld.Fade(false);
        }

        // We use character's X and Z to stay PERFECTLY centered
        // And use the icon's Y (or fallback) for height
        Vector3 spawnPos = new Vector3(charObj.transform.position.x, spawnY, charObj.transform.position.z);

        _activeBigCard = Instantiate(_cardPrefabForUI, spawnPos, Quaternion.identity);
        _activeBigCardKey = key;

        // Force World Space Canvas FIRST so we can work with it properly
        Canvas canvas = _activeBigCard.GetComponent<Canvas>();
        if (canvas == null) canvas = _activeBigCard.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Add components for interaction and billboarding
        if (_activeBigCard.GetComponent<GraphicRaycaster>() == null) _activeBigCard.AddComponent<GraphicRaycaster>();
        _activeBigCard.AddComponent<BillboardUI>();

        // Set World Scale
        // Increased to 2.2f for a real "BIG" card feel
        float worldScale = 0.003f * 2.2f; 
        _activeBigCard.transform.localScale = Vector3.one * worldScale;

        // RE-FIX: Ensure it stays EXACTLY at spawnPos
        _activeBigCard.transform.position = spawnPos;

        // Disable UI-specific scaling/flipping logic
        var scaler = _activeBigCard.GetComponent<CardScaler>();
        if (scaler != null) scaler.enabled = false;
        var flip = _activeBigCard.GetComponent<CardFlipController>();
        if (flip != null) flip.enabled = false;

        CardInfo info = _activeBigCard.GetComponent<CardInfo>();
        if (info != null)
        {
            info.CharData = data;
            info.Initialize();
            
            // Populate mods
            var modsContainer = _activeBigCard.GetComponentInChildren<ModsCardContainer>();
            if (modsContainer != null)
            {
                if (_spawnedCharModIndices.TryGetValue(key, out int[] modIndices))
                {
                    bool isEnemy = (ownerID != _localPlayerIndex);
                    bool isRevealed = _revealedEnemyMods.Contains(key);

                    foreach (var idx in modIndices)
                    {
                        if (idx >= 0 && idx < _library.AllMods.Count)
                        {
                            var mod = _library.AllMods[idx];
                            if (!isEnemy || isRevealed)
                            {
                                modsContainer.AddMod(mod);
                            }
                        }
                    }

                    if (isEnemy && !isRevealed)
                    {
                        _revealedEnemyMods.Add(key);
                    }
                }
            }
        }

        // Add a click handler to the big card itself to allow closing it
        var worldClickHandler = _activeBigCard.AddComponent<CharacterWorldClickHandler>();
        worldClickHandler.Initialize(ownerID, pairID, data);

        // Animation for opening - we need to adjust the Coroutine to handle the world scale
        StartCoroutine(ScaleAnimation(_activeBigCard.transform, worldScale));
    }

    private void HideBigCard()
    {
        if (_activeBigCardKey != (-1, -1))
        {
            // Перевіряємо charObj на null, бо він міг бути вже знищений або ми самі його щойно знесли
            if (_spawnedCharacters.TryGetValue(_activeBigCardKey, out GameObject charObj) && charObj != null)
            {
                var iconWorld = charObj.GetComponentInChildren<PlayerIconWorld>();
                if (iconWorld != null) iconWorld.Fade(true);
            }
        }

        if (_activeBigCard != null)
        {
            Destroy(_activeBigCard);
            _activeBigCard = null;
        }
        _activeBigCardKey = (-1, -1);
    }

    private IEnumerator ScaleAnimation(Transform target, float targetScale)
    {
        float duration = 0.3f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            target.localScale = Vector3.one * Mathf.Lerp(0f, targetScale, t);
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one * targetScale;
    }
    #endregion

    #endregion

    #region SYNCHRONIZATION FOR LATE JOINERS

    [PunRPC]
    private void RPC_RequestCurrentPlacements(PhotonMessageInfo info)
    {
        Debug.Log($"[Placement] Player {info.Sender.ActorNumber} requested current placements. Sending mine...");
        foreach (var kvp in _spawnedCharLibIndices)
        {
            var key = kvp.Key; // (ownerID, pairID)
            int libIdx = kvp.Value;
            int[] mods = _spawnedCharModIndices.TryGetValue(key, out var m) ? m : new int[0];

            // Only send characters that WE (this client) are responsible for
            if (key.Item1 == _localPlayerIndex)
            {
                var gridPosEntry = _tileOccupants.FirstOrDefault(x => x.Value == key);
                if (gridPosEntry.Value == key)
                {
                    photonView.RPC("RPC_PlaceCharacter", info.Sender, key.Item1, key.Item2, libIdx, mods, gridPosEntry.Key.x, gridPosEntry.Key.y);
                }
            }
        }
    }

    #endregion

    #region PREVIEW / HOLOGRAM LOGIC (Networked)

    public void ShowPreview(CardDragHandler card, Tile tile)
    {
        if (tile == null || tile.Type == TileType.Impassable || tile.PlacementOwnerID != card.OwnerID)
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

    public int GetLibraryIndex(CharacterData data) => _library != null ? _library.AllCharacters.IndexOf(data) : -1;

    private CardDragHandler GetActiveCardForUnit(int ownerID, int pairID)
    {
        if (_spawnedCharLibIndices.TryGetValue((ownerID, pairID), out int currentLibIdx))
        {
            // 1. Try active controller first (for current player)
            if (_deckController != null)
            {
                var card = _deckController.GetActiveCardHandler(ownerID, pairID);
                if (card != null && GetLibraryIndex(card.GetComponent<CardInfo>()?.CharData) == currentLibIdx)
                    return card;
            }

            // 2. Fallback: Search all cards (incl. inactive for Hotseat victim)
            var allCards = FindObjectsOfType<CardDragHandler>(true);
            return allCards.FirstOrDefault(c => 
                c.OwnerID == ownerID && 
                c.PairID == pairID && 
                c.GetComponent<CardInfo>() != null &&
                GetLibraryIndex(c.GetComponent<CardInfo>().CharData) == currentLibIdx);
        }
        return null;
    }

    private Tile FindTileAt(Vector2Int pos)
    {
        if (GridManager.Instance != null) return GridManager.Instance.GetTile(pos);
        return FindObjectsOfType<Tile>().FirstOrDefault(t => t.GridCoordinates == pos);
    }

    private Color GetColorForPlayer(int ownerID)
    {
        // Player 1 = Green, Player 2 = Red.
        return ownerID == 1 ? Color.green : Color.red;
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
        float tileSize = 1.25f;
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
