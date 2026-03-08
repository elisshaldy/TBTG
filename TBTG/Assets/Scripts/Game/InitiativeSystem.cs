using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Linq;

public class InitiativeSystem : MonoBehaviour, IDropHandler
{
    public static InitiativeSystem Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform _containerInitiative;
    [SerializeField] private GameObject _initiativePrefab;
    [SerializeField] private Button _acceptBtn;
    [SerializeField] private Sprite _unknownCharSprite;
    [SerializeField] private TextMeshProUGUI _roundText;

    [Header("State")]
    private List<int> _initiativeQueue = new List<int>(); // Локальна черга (PairIDs)
    private HashSet<int> _addedPairIDs = new HashSet<int>(); 
    
    private Dictionary<int, List<int>> _allPlayersInitiatives = new Dictionary<int, List<int>>();
    private List<(int ownerID, int pairID)> _finalQueue = new List<(int, int)>();
    public bool IsFinalized => _isFinalized;
    private bool _isFinalized = false;
    private bool _p1Starts;
    private int _roundCount = 1;
    private int _unitsActedInRound = 0;
    
    private int _actionsRemaining = 2;
    private const int MAX_ACTIONS = 2;

    public int ActionsRemaining => _actionsRemaining;

    private CardDeckController _deckController;
    private GameSceneState _gameSceneState;
    private GameDataLibrary _library;
    private PhotonView _photonView;

    public (int ownerID, int pairID) GetActiveUnitKey()
    {
        if (_isFinalized && _finalQueue.Count > 0) return _finalQueue[0];
        return (-1, -1);
    }

    public int CurrentTurnPlayerID 
    {
        get
        {
            if (_isFinalized && _finalQueue.Count > 0) return _finalQueue[0].ownerID;
            if (_gameSceneState != null && _gameSceneState._currentSettings != null) return _gameSceneState._currentSettings.CurrentPlayerIndex;
            return 1;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        _deckController = FindObjectOfType<CardDeckController>();
        _gameSceneState = FindObjectOfType<GameSceneState>();
        _photonView = GetComponent<PhotonView>();
        
        if (CharacterPlacementManager.Instance != null)
        {
            // Get library from PlacementManager to ensure consistency
            var libField = CharacterPlacementManager.Instance.GetType().GetField("_library", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _library = libField?.GetValue(CharacterPlacementManager.Instance) as GameDataLibrary;
        }

        if (_deckController != null)
        {
            _deckController.DeckStateChanged += (isFull) => UpdateInitiativeUI();
        }

        if (_acceptBtn != null)
        {
            _acceptBtn.onClick.AddListener(OnAcceptClick);
            _acceptBtn.gameObject.SetActive(false);
        }

        if (_roundText != null) _roundText.gameObject.SetActive(false);
        LocalizationManager.OnLanguageChanged += UpdateRoundUI;
    }

    private void OnDestroy()
    {
        if (_deckController != null)
        {
            _deckController.DeckStateChanged -= (isFull) => UpdateInitiativeUI();
        }
        LocalizationManager.OnLanguageChanged -= UpdateRoundUI;
    }

    public bool WasDropHandled { get; private set; }

    public void ResetDropFlag()
    {
        WasDropHandled = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_isFinalized) return;

        WasDropHandled = false;
        if (eventData.pointerDrag == null) return;

        CardDragHandler card = eventData.pointerDrag.GetComponent<CardDragHandler>();
        InitiativeEntryDragHandler entry = eventData.pointerDrag.GetComponent<InitiativeEntryDragHandler>();

        if (card != null || entry != null)
        {
            int pairID = card != null ? card.PairID : entry.PairID;
            
            // ... (ліміти і перевірки) ...
            int maxPairs = 0;
            if (_deckController != null) 
            {
                var deckField = _deckController.GetType().GetField("_cardDeck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var deck = deckField?.GetValue(_deckController) as List<CardSlot>;
                if (deck != null) maxPairs = deck.Count / 2;
            }

            bool isAlreadyInQueue = _addedPairIDs.Contains(pairID);
            if (!isAlreadyInQueue && _initiativeQueue.Count >= maxPairs && maxPairs > 0) return;

            int insertIndex = CalculateInsertIndex(eventData.position);

            if (isAlreadyInQueue)
            {
                _initiativeQueue.Remove(pairID);
            }
            else
            {
                _addedPairIDs.Add(pairID);
            }

            if (insertIndex > _initiativeQueue.Count) insertIndex = _initiativeQueue.Count;
            _initiativeQueue.Insert(insertIndex, pairID);
            
            // NEW: If we drop a card from a pair, it MUST become the active member of that pair
            if (card != null && _deckController != null)
            {
                _deckController.MakeActive(card);
            }

            WasDropHandled = true;
            UpdateInitiativeUI();
            UpdateAcceptButton();

            if (PersistentMusicManager.Instance != null) 
                PersistentMusicManager.Instance.PlayCardPlaced();
        }
    }

    public void UpdateAcceptButton()
    {
        if (_acceptBtn == null || _isFinalized) return;
        
        int maxPairs = 0;
        if (_deckController != null) 
        {
            var deckField = _deckController.GetType().GetField("_cardDeck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var deck = deckField?.GetValue(_deckController) as List<CardSlot>;
            if (deck != null) maxPairs = deck.Count / 2;
        }
        if (maxPairs == 0) maxPairs = 4;

        bool isInitiativeFull = _initiativeQueue.Count >= maxPairs;

        int currentPlayerId = 1;
        if (PhotonNetwork.InRoom)
        {
            currentPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
        }
        else if (_gameSceneState != null && _gameSceneState._currentSettings is HotseatSettings hs)
        {
            currentPlayerId = hs.CurrentPlayerIndex;
        }

        bool areUnitsPlaced = false;
        if (CharacterPlacementManager.Instance != null)
        {
            areUnitsPlaced = CharacterPlacementManager.Instance.GetPlacedCharacterCount(currentPlayerId) >= maxPairs;
        }

        _acceptBtn.gameObject.SetActive(isInitiativeFull && areUnitsPlaced && maxPairs > 0);
    }

    private void OnAcceptClick()
    {
        if (_isFinalized) return;
        
        if (_gameSceneState != null && _gameSceneState._currentSettings is HotseatSettings hs)
        {
            int currentPlayer = hs.CurrentPlayerIndex;
            _allPlayersInitiatives[currentPlayer] = new List<int>(_initiativeQueue);
            
            if (currentPlayer == 1)
            {
                // Transition to Player 2 instead of finishing
                _initiativeQueue.Clear();
                _addedPairIDs.Clear();
                
                hs.AdvanceToPlayer2Map(FindObjectOfType<GameUIController>());
                
                UpdateInitiativeUI();
                UpdateAcceptButton();
            }
            else
            {
                FinalizeInitiative();
            }
        }
        else if (_gameSceneState != null && _gameSceneState._currentSettings is PlayerVsBotSettings botSettings)
        {
            _allPlayersInitiatives[1] = new List<int>(_initiativeQueue);
            
            // Collect all placed pair IDs for Bot (Player 2)
            List<int> botAvailablePairs = new List<int>();
            if (CharacterPlacementManager.Instance != null)
            {
                // In Bot mode, Player 2 is always the bot
                for (int i = 0; i < 10; i++) // Check up to 10 potential pairs
                {
                    if (CharacterPlacementManager.Instance.IsPairPlaced(2, i))
                        botAvailablePairs.Add(i);
                }
            }
            
            // Randomly shuffle bot's initiative
            for (int i = 0; i < botAvailablePairs.Count; i++) {
                int temp = botAvailablePairs[i];
                int randomIndex = Random.Range(i, botAvailablePairs.Count);
                botAvailablePairs[i] = botAvailablePairs[randomIndex];
                botAvailablePairs[randomIndex] = temp;
            }
            _allPlayersInitiatives[2] = botAvailablePairs;
            FinalizeInitiative();
        }
        else if (PhotonNetwork.InRoom && _photonView != null)
        {
            _photonView.RPC("RPC_SubmitInitiative", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, _initiativeQueue.ToArray());
            _acceptBtn.gameObject.SetActive(false);
        }
    }

    [PunRPC]
    private void RPC_SubmitInitiative(int playerID, int[] pairIDs)
    {
        _allPlayersInitiatives[playerID] = new List<int>(pairIDs);
        
        if (_allPlayersInitiatives.Count >= 2)
        {
            FinalizeInitiative();
        }
    }

    private void FinalizeInitiative()
    {
        _acceptBtn?.gameObject.SetActive(false);

        // Визначаємо, хто перший (для мультиплеєра використовуємо стабільний рандом)
        if (PhotonNetwork.InRoom)
        {
            // Використовуємо номер кімнати як зерно для синхронного рандому
            Random.InitState(PhotonNetwork.CurrentRoom.Name.GetHashCode());
            _p1Starts = Random.value > 0.5f;
        }
        else
        {
            _p1Starts = Random.value > 0.5f;
        }

        List<int> p1List, p2List;

        if (PhotonNetwork.InRoom)
        {
            var keys = _allPlayersInitiatives.Keys.OrderBy(k => k).ToList();
            p1List = _allPlayersInitiatives[keys[0]];
            p2List = _allPlayersInitiatives[keys[1]];
        }
        else
        {
            p1List = _allPlayersInitiatives.ContainsKey(1) ? _allPlayersInitiatives[1] : new List<int>();
            p2List = _allPlayersInitiatives.ContainsKey(2) ? _allPlayersInitiatives[2] : new List<int>();
        }

        _finalQueue.Clear();
        int max = Mathf.Max(p1List.Count, p2List.Count);
        for (int i = 0; i < max; i++)
        {
            if (_p1Starts)
            {
                if (i < p1List.Count) _finalQueue.Add((PhotonNetwork.InRoom ? _allPlayersInitiatives.Keys.OrderBy(k => k).First() : 1, p1List[i]));
                if (i < p2List.Count) _finalQueue.Add((PhotonNetwork.InRoom ? _allPlayersInitiatives.Keys.OrderBy(k => k).Last() : 2, p2List[i]));
            }
            else
            {
                if (i < p2List.Count) _finalQueue.Add((PhotonNetwork.InRoom ? _allPlayersInitiatives.Keys.OrderBy(k => k).Last() : 2, p2List[i]));
                if (i < p1List.Count) _finalQueue.Add((PhotonNetwork.InRoom ? _allPlayersInitiatives.Keys.OrderBy(k => k).First() : 1, p1List[i]));
            }
        }

        // Hotseat: Initialize the first player's deck once the order is decided
        if (_gameSceneState != null && _gameSceneState._currentSettings is HotseatSettings hsGame)
        {
            if (_finalQueue.Count > 0)
            {
                int firstPlayer = _finalQueue[0].ownerID;
                hsGame.MapPlayerIndex = firstPlayer;
                
                var initializer = FindObjectOfType<GameDataInitializer>();
                if (initializer != null)
                {
                    initializer.RefreshBattleUI();
                }
            }
        }

        _isFinalized = true;
        _roundCount = 1;
        _unitsActedInRound = 0;
        if (_roundText != null) _roundText.gameObject.SetActive(true);
        UpdateRoundUI();

        if (CharacterPlacementManager.Instance != null)
        {
            // CharacterPlacementManager.Instance.InitializeHPs();
        }

        UpdateInitiativeUI();
    }

    public void RemovePair(int pairID)
    {
        if (_addedPairIDs.Contains(pairID))
        {
            _initiativeQueue.Remove(pairID);
            _addedPairIDs.Remove(pairID);
            UpdateInitiativeUI();
            
            if (PersistentMusicManager.Instance != null) 
                PersistentMusicManager.Instance.PlayCardReturned();
        }
    }

    private int CalculateInsertIndex(Vector2 screenPos)
    {
        int validIndex = 0;
        for (int i = 0; i < _containerInitiative.childCount; i++)
        {
            RectTransform child = _containerInitiative.GetChild(i) as RectTransform;
            
            // Якщо ми перетягуємо сам елемент черги, проігноруємо його власну стару плашку
            if (UnityEngine.EventSystems.EventSystem.current.alreadySelecting && 
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == child.gameObject) continue;

            // ВАЖЛИВО: Пропускаємо скриті картки ворога (у них drag.enabled == false)
            var drag = child.GetComponent<InitiativeEntryDragHandler>();
            if (drag != null && !drag.enabled) continue;

            if (screenPos.y > child.position.y) 
            {
                return validIndex;
            }
            validIndex++;
        }
        return validIndex;
    }

    public void ConsumeAction(bool isEndingTurnAction = false)
    {
        if (!_isFinalized) return;

        if (isEndingTurnAction)
        {
            _actionsRemaining = 0;
        }
        else
        {
            _actionsRemaining--;
        }

        if (_actionsRemaining <= 0)
        {
            NextTurn();
        }
        else
        {
            UpdateInitiativeUI();
        }
    }

    public void NextTurn()
    {
        if (!_isFinalized || _finalQueue.Count == 0) return;

        int prevPlayerID = CurrentTurnPlayerID;

        // 1. REMOVE the finished unit (as requested)
        _finalQueue.RemoveAt(0);
        _unitsActedInRound++;

        // 2. CHECK: If round is over, refill the queue
        if (_finalQueue.Count == 0)
        {
            RegenerateRound();
        }

        _actionsRemaining = MAX_ACTIONS;
        
        // Reset movement mode if active
        if (CharacterPlacementManager.Instance != null)
            CharacterPlacementManager.Instance.ClearMovementMode();

        UpdateInitiativeUI();

        // REFRESH: Swap the movement containers ONLY if the player changed
        if (GameDataInitializer.Instance != null && CurrentTurnPlayerID != prevPlayerID)
        {
            GameDataInitializer.Instance.RefreshBattleUI();
        }
    }

    private void RegenerateRound()
    {
        _roundCount++;
        _unitsActedInRound = 0;
        UpdateRoundUI();

        List<int> p1List, p2List;
        if (PhotonNetwork.InRoom)
        {
            var keys = _allPlayersInitiatives.Keys.OrderBy(k => k).ToList();
            p1List = _allPlayersInitiatives[keys[0]];
            p2List = _allPlayersInitiatives[keys[1]];
        }
        else
        {
            p1List = _allPlayersInitiatives.ContainsKey(1) ? _allPlayersInitiatives[1] : new List<int>();
            p2List = _allPlayersInitiatives.ContainsKey(2) ? _allPlayersInitiatives[2] : new List<int>();
        }

        int max = Mathf.Max(p1List.Count, p2List.Count);
        for (int i = 0; i < max; i++)
        {
            if (_p1Starts)
            {
                if (i < p1List.Count) _finalQueue.Add((PhotonNetwork.InRoom ? _allPlayersInitiatives.Keys.OrderBy(k => k).First() : 1, p1List[i]));
                if (i < p2List.Count) _finalQueue.Add((PhotonNetwork.InRoom ? _allPlayersInitiatives.Keys.OrderBy(k => k).Last() : 2, p2List[i]));
            }
            else
            {
                if (i < p2List.Count) _finalQueue.Add((PhotonNetwork.InRoom ? _allPlayersInitiatives.Keys.OrderBy(k => k).Last() : 2, p2List[i]));
                if (i < p1List.Count) _finalQueue.Add((PhotonNetwork.InRoom ? _allPlayersInitiatives.Keys.OrderBy(k => k).First() : 1, p1List[i]));
            }
        }
    }

    private void UpdateRoundUI()
    {
        if (!_isFinalized) return;

        if (_roundText != null)
        {
            string translated = LocalizationManager.GetTranslation("round_txt");
            _roundText.text = $"{translated}: {_roundCount}";

            // FIX: If there's a LocalizationLabel on the same object, it might overwrite our text.
            // We set its suffix to ensure the number persists during language changes.
            var locLabel = _roundText.GetComponent<LocalizationLabel>();
            if (locLabel != null)
            {
                locLabel.Key = "round_txt";
                locLabel.SetSuffix(": " + _roundCount);
            }
        }
    }

    public void RemoveFromQueue(int ownerID, int pairID)
    {
        _finalQueue.RemoveAll(q => q.ownerID == ownerID && q.pairID == pairID);
        UpdateInitiativeUI();
        
        if (_finalQueue.Count == 0)
        {
            Debug.Log("[Game Over] Total Victory for the survivors!");
            // Потім можна завантажити переможний екран
        }
    }

    public void UpdateInitiativeUI()
    {
        // Safe clear: Detach children so childCount becomes 0 immediately 
        // to avoid duplication if called multiple times in one frame.
        for (int i = _containerInitiative.childCount - 1; i >= 0; i--)
        {
            Destroy(_containerInitiative.GetChild(i).gameObject);
        }
        _containerInitiative.DetachChildren();

        if (_isFinalized)
        {
            int myID = 1;
            if (PhotonNetwork.InRoom)
            {
                myID = PhotonNetwork.LocalPlayer.ActorNumber;
            }
            else if (_gameSceneState != null && _gameSceneState._currentSettings is HotseatSettings hsGame)
            {
                myID = hsGame.CurrentPlayerIndex;
            }

            for (int i = 0; i < _finalQueue.Count; i++)
            {
                var entry = _finalQueue[i];

                if (CharacterPlacementManager.Instance != null)
                {
                    bool isActive = (i == 0);
                    CharacterPlacementManager.Instance.SetCharacterActive(entry.ownerID, entry.pairID, isActive);
                    
                    if (isActive)
                    {
                        bool isHotseat = (PhotonNetwork.InRoom == false);
                        bool isMyTurn = isHotseat || entry.ownerID == myID;
                        
                        // NEW: Camera following (Hotseat: Always, Multiplayer: Only if it's Me)
                        if (PlayerCameraController.Instance != null)
                        {
                            if (isMyTurn)
                            {
                                var characterObj = CharacterPlacementManager.Instance.GetCharacterObject(entry.ownerID, entry.pairID);
                                if (characterObj != null) PlayerCameraController.Instance.FollowTarget(characterObj.transform);
                            }
                            else
                            {
                                PlayerCameraController.Instance.ResetToMapCenter();
                            }
                        }
                        
                        // NEW: Toggle Skip Button visibility
                        var uiController = FindObjectOfType<GameUIController>();
                        if (uiController != null) uiController.SetSkipTurnButtonActive(isMyTurn);
                    }
                }

                CharacterData data = GetCharacterDataForFinal(entry.ownerID, entry.pairID);
                
                if (data != null)
                {
                    GameObject obj = Instantiate(_initiativePrefab, _containerInitiative);
                    
                    bool isMine = (entry.ownerID == myID);
                    Sprite displaySprite = data.CharacterSprite;

                    // Логіка: своїх бачимо завжди. 
                    // Ворога - ТІЛЬКИ якщо він зараз перший у черзі (його хід).
                    if (!isMine && i > 0)
                    {
                        displaySprite = _unknownCharSprite;
                    }

                    SetupInitiativeEntry(obj, displaySprite, i + 1 + _unitsActedInRound, entry.pairID);
                    
                    var drag = obj.GetComponent<InitiativeEntryDragHandler>();
                    if (drag != null) drag.enabled = false;
                }
            }
        }
        else
        {

            for (int i = 0; i < _initiativeQueue.Count; i++)
            {
                int pID = _initiativeQueue[i];
                CharacterData activeData = _deckController != null ? _deckController.GetActiveCharacterData(pID) : null;

                if (activeData != null)
                {
                    GameObject entryObj = Instantiate(_initiativePrefab, _containerInitiative);
                    // Використовуємо i + 1, щоб у гравця завжди було 1-4, 
                    // незалежно від того, скільки там прихованих іконок іншого гравця
                    SetupInitiativeEntry(entryObj, activeData.CharacterSprite, i + 1, pID);
                }
            }
        }
    }

    private CharacterData GetCharacterDataForFinal(int ownerID, int pairID)
    {
        // 1. Direct call to CharacterPlacementManager (Source of truth for what's on the field)
        if (CharacterPlacementManager.Instance != null && _library != null)
        {
            int libIdx = CharacterPlacementManager.Instance.GetSpawnedCharacterLibIndex(ownerID, pairID);
            if (libIdx >= 0 && libIdx < _library.AllCharacters.Count)
                return _library.AllCharacters[libIdx];
        }

        // 2. Fallback to deck (only if not on field yet or during setup)
        if (_deckController != null)
        {
            return _deckController.GetActiveCharacterData(pairID);
        }

        return null;
    }

    private void OnEnable() => StartCoroutine(SubscribeRoutine());
    private void OnDisable() { if (_deckController != null) _deckController.DeckStateChanged -= RefreshUIOnDeckUpdate; }

    private IEnumerator SubscribeRoutine()
    {
        while (_deckController == null) { _deckController = FindObjectOfType<CardDeckController>(); yield return null; }
        _deckController.DeckStateChanged += RefreshUIOnDeckUpdate;
    }

    private void RefreshUIOnDeckUpdate(bool _) { UpdateInitiativeUI(); }

    private void SetupInitiativeEntry(GameObject entryObj, Sprite sprite, int order, int pID)
    {
        var icon = entryObj.GetComponentInChildren<Image>();
        var text = entryObj.GetComponentInChildren<TextMeshProUGUI>();

        if (icon != null) 
        {
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
            }
            else
            {
                // Заглушка: якщо спрайту немає, робимо іконку просто темним колом/квадратом
                icon.sprite = null;
                icon.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }
        }
        
        if (text != null) 
        {
            // Якщо порядок 0 - це прихована іконка іншого гравця
            text.text = (order > 0) ? order.ToString() : "?";
        }

        // Додаємо або налаштовуємо скрипт для перетягування самої іконки
        var drag = entryObj.GetComponent<InitiativeEntryDragHandler>();
        if (drag == null) drag = entryObj.AddComponent<InitiativeEntryDragHandler>();
        drag.PairID = pID;
    }

    public List<CharacterData> GetQueue() 
    {
        List<CharacterData> result = new List<CharacterData>();
        
        if (_isFinalized)
        {
            foreach (var entry in _finalQueue)
            {
                var data = GetCharacterDataForFinal(entry.ownerID, entry.pairID);
                if (data != null) result.Add(data);
            }
        }
        else
        {
            foreach(var pID in _initiativeQueue)
            {
                var data = _deckController.GetActiveCharacterData(pID);
                if (data != null) result.Add(data);
            }
        }
        return result;
    }

    public void ClearInitiative()
    {
        _initiativeQueue.Clear();
        _addedPairIDs.Clear();
        _allPlayersInitiatives.Clear();
        _finalQueue.Clear();
        _isFinalized = false;
        UpdateInitiativeUI();
    }
}