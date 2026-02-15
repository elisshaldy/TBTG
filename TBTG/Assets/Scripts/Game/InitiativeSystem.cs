using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("State")]
    private List<int> _initiativeQueue = new List<int>(); // Локальна черга (PairIDs)
    private HashSet<int> _addedPairIDs = new HashSet<int>(); 
    
    private Dictionary<int, List<int>> _allPlayersInitiatives = new Dictionary<int, List<int>>();
    private List<(int ownerID, int pairID)> _finalQueue = new List<(int, int)>();
    private bool _isFinalized = false;

    private CardDeckController _deckController;
    private GameSceneState _gameSceneState;
    private PhotonView _photonView;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        _deckController = FindObjectOfType<CardDeckController>();
        _gameSceneState = FindObjectOfType<GameSceneState>();
        _photonView = GetComponent<PhotonView>();
        
        if (_deckController != null)
        {
            _deckController.DeckStateChanged += (isFull) => UpdateInitiativeUI();
        }

        if (_acceptBtn != null)
        {
            _acceptBtn.onClick.AddListener(OnAcceptClick);
            _acceptBtn.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_deckController != null)
        {
            _deckController.DeckStateChanged -= (isFull) => UpdateInitiativeUI();
        }
    }

    public bool WasDropHandled { get; private set; }

    public void ResetDropFlag()
    {
        WasDropHandled = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
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
            
            WasDropHandled = true;
            UpdateInitiativeUI();
            UpdateAcceptButton();

            if (PersistentMusicManager.Instance != null) 
                PersistentMusicManager.Instance.PlayCardPlaced();
        }
    }

    private void UpdateAcceptButton()
    {
        if (_acceptBtn == null || _isFinalized) return;
        
        // Кнопка з'являється тільки коли черга повна (4 карти)
        int maxPairs = 4;
        _acceptBtn.gameObject.SetActive(_initiativeQueue.Count >= maxPairs);
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
                // Очищаємо для другого гравця
                _initiativeQueue.Clear();
                _addedPairIDs.Clear();
                UpdateInitiativeUI();
                UpdateAcceptButton();
                _gameSceneState.Next(FindObjectOfType<GameUIController>());
            }
            else
            {
                FinalizeInitiative();
            }
        }
        else if (_gameSceneState != null && _gameSceneState._currentSettings is PlayerVsBotSettings botSettings)
        {
            _allPlayersInitiatives[1] = new List<int>(_initiativeQueue);
            // Бот генерує рандомну ініціативу
            List<int> botList = new List<int> { 0, 1, 2, 3 };
            for (int i = 0; i < 4; i++) {
                int temp = botList[i];
                int randomIndex = Random.Range(i, 4);
                botList[i] = botList[randomIndex];
                botList[randomIndex] = temp;
            }
            _allPlayersInitiatives[2] = botList;
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
        _isFinalized = true;
        _acceptBtn?.gameObject.SetActive(false);

        // Визначаємо, хто перший (для мультиплеєра використовуємо стабільний рандом)
        bool p1Starts;
        if (PhotonNetwork.InRoom)
        {
            // Використовуємо номер кімнати як зерно для синхронного рандому
            Random.InitState(PhotonNetwork.CurrentRoom.Name.GetHashCode());
            p1Starts = Random.value > 0.5f;
        }
        else
        {
            p1Starts = Random.value > 0.5f;
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
            if (p1Starts)
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
        for (int i = 0; i < _containerInitiative.childCount; i++)
        {
            RectTransform child = _containerInitiative.GetChild(i) as RectTransform;
            // Якщо ми перетягуємо сам елемент черги, проігноруємо його власну стару плашку
            if (UnityEngine.EventSystems.EventSystem.current.alreadySelecting && 
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == child.gameObject) continue;

            if (screenPos.y > child.position.y) 
            {
                return i;
            }
        }
        return _containerInitiative.childCount;
    }

    public void UpdateInitiativeUI()
    {
        foreach (Transform child in _containerInitiative)
        {
            Destroy(child.gameObject);
        }

        if (_isFinalized)
        {
            for (int i = 0; i < _finalQueue.Count; i++)
            {
                var entry = _finalQueue[i];
                CharacterData data = GetCharacterDataForFinal(entry.ownerID, entry.pairID);
                if (data != null)
                {
                    GameObject obj = Instantiate(_initiativePrefab, _containerInitiative);
                    SetupInitiativeEntry(obj, data, i + 1, entry.pairID);
                    // Вимикаємо драг для фінальної черги
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
                    SetupInitiativeEntry(entryObj, activeData, i + 1, pID);
                }
            }
        }
    }

    private CharacterData GetCharacterDataForFinal(int ownerID, int pairID)
    {
        // Якщо це ми - беремо з нашої деки (можемо поміняти на лету)
        int myID = PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber : 1;
        if (ownerID == myID && _deckController != null)
        {
            return _deckController.GetActiveCharacterData(pairID);
        }

        // Якщо це ворог - беремо з CharacterPlacementManager (він знає libIndex)
        if (CharacterPlacementManager.Instance != null)
        {
            // В Hotseat ownerID може бути 1 або 2
            // В мультиплеєрі це ActorNumber
            
            // Нам треба знайти CharacterData за libIndex, який зберіг PlacementManager
            // (Це безпечно, бо модель на полі вже стоїть)
            var spawnedIndices = CharacterPlacementManager.Instance.GetType()
                .GetField("_spawnedCharLibIndices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(CharacterPlacementManager.Instance) as Dictionary<(int, int), int>;

            if (spawnedIndices != null && spawnedIndices.TryGetValue((ownerID, pairID), out int libIdx))
            {
                var library = CharacterPlacementManager.Instance.GetType()
                    .GetField("_library", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(CharacterPlacementManager.Instance) as GameDataLibrary;
                
                if (library != null && libIdx >= 0 && libIdx < library.AllCharacters.Count)
                    return library.AllCharacters[libIdx];
            }
        }
        return null;
    }

    private void SetupInitiativeEntry(GameObject entryObj, CharacterData data, int order, int pID)
    {
        var icon = entryObj.GetComponentInChildren<Image>();
        var text = entryObj.GetComponentInChildren<TextMeshProUGUI>();

        if (icon != null) icon.sprite = data.CharacterSprite;
        if (text != null) text.text = order.ToString();

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