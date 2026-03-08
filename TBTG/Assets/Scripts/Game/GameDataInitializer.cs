using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using System.Collections;

public class GameDataInitializer : MonoBehaviour
{
    public static GameDataInitializer Instance { get; private set; }

    [Header("Data Source")]
    [SerializeField] private GameDataLibrary _library;
    
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private GameObject _modPrefab;
    [SerializeField] private GameObject _movementCardPrefab;
    
    [SerializeField] private LayoutGroup _cardContainer;
    [SerializeField] private LayoutGroup _modContainer;
    [SerializeField] private LayoutGroup _movementCardContainer;
    [SerializeField] private LayoutGroup _movementCardContainerEnemy;
    [SerializeField] private CardDeckController _deckController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Header("Generation Settings")]
    [SerializeField] private int _cardsToSpawn = 10;
    [SerializeField] private int _modsToSpawn = 35;
    [SerializeField] private int _movementCardsToSpawn = 6;

    private List<CardInfo> _cardsInstances = new List<CardInfo>();
    private List<ModInfo> _modsInstances = new List<ModInfo>();
    private List<MovementCardInfo> _movementCardsInstances = new List<MovementCardInfo>();

    public void InitializeGame()
    {
        if (_library == null)
        {
            Debug.LogError("GameDataLibrary is NULL");
            return;
        }

        // 1. Отримуємо налаштування та індекс поточного гравця із GameSceneState (джерело правди в сцені)
        GameSceneState sceneState = FindObjectOfType<GameSceneState>();
        GameSettings settings = null;
        SceneState mode = SceneState.Undefined;

        if (sceneState != null && sceneState._currentSettings != null)
        {
            settings = sceneState._currentSettings;
            mode = sceneState.CurrentStep == GameSetupStep.Cards ? SceneState.Hotseat : SceneState.Undefined; // Placeholder logic
            // Але краще візьмемо режим прямо з менеджера, якщо він є
            if (GameSettingsManager.Instance != null) mode = GameSettingsManager.Instance.CurrentMode;
        }
        else if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.CurrentSettings != null)
        {
            settings = GameSettingsManager.Instance.CurrentSettings;
            mode = GameSettingsManager.Instance.CurrentMode;
        }

        int playerIndex = GetPlayerIndex(mode, settings);
        // Debug.Log($"InitializeGame: PlayerIndex = {playerIndex}, Mode = {mode}");

        // NEW: Rotate camera to player's side at the start of their setup
        if (PlayerCameraController.Instance != null)
        {
            PlayerCameraController.Instance.RotateToPlayer(playerIndex);
        }

        // 2. Отримуємо індекси персонажів
        // Якщо індекси ще не згенеровані (наприклад, у Hotseat або на старті), генеруємо їх один раз для сесії
        if (settings != null && (settings.CharacterPoolIndices == null || settings.CharacterPoolIndices.Length == 0))
        {
            settings.CharacterPoolIndices = _library.GetShuffledIndices();
            // Debug.Log($"Generated CharacterPoolIndices for session. Total characters: {settings.CharacterPoolIndices.Length}");
        }

        int[] myIndices = (settings != null) ? settings.GetIndicesForPlayer(playerIndex) : null;
        List<CharacterData> myChars = new List<CharacterData>();

        if (myIndices != null)
        {
            foreach (int idx in myIndices)
            {
                if (idx < _library.AllCharacters.Count)
                    myChars.Add(_library.AllCharacters[idx]);
            }
        }
        else
        {
            // Якщо налаштувань немає взагалі, беремо просто рандом
            myChars = _library.GetRandomCharacters(_cardsToSpawn);
        }

        // ===== CHARACTERS =====
        if (_cardContainer != null) _cardContainer.enabled = true;
        ClearContainer(_cardContainer.transform);
        _cardsInstances.Clear();

        for (int i = 0; i < _cardsToSpawn; i++)
        {
            GameObject cardObj = Instantiate(_cardPrefab, _cardContainer.transform);
            CardInfo cardInfo = cardObj.GetComponent<CardInfo>();
            if (cardInfo != null)
            {
                if (i < myChars.Count) cardInfo.CharData = myChars[i];
                cardInfo.Initialize();
                _cardsInstances.Add(cardInfo);

                // Реєструємо картку у контролері деки
                if (_deckController != null)
                {
                    var dragHandler = cardObj.GetComponent<CardDragHandler>();
                    dragHandler.CardID = i;
                    dragHandler.OwnerID = playerIndex;
                    dragHandler.PairID = i / 2;
                    dragHandler.SetDependencies(sceneState, _deckController);
                    _deckController.RegisterCard(dragHandler);
                }
            }
        }

        // ===== MODIFIERS =====
        if (_modContainer != null) _modContainer.enabled = true;
        ClearContainer(_modContainer.transform);
        _modsInstances.Clear();
        List<ModData> randomMods = _library.GetModsByBalanceRules();

        for (int i = 0; i < _modsToSpawn; i++)
        {
            GameObject modObj = Instantiate(_modPrefab, _modContainer.transform);
            ModInfo modInfo = modObj.GetComponent<ModInfo>();
            if (modInfo != null)
            {
                if (i < randomMods.Count) modInfo.ModData = randomMods[i];
                modInfo.Initialize();
                _modsInstances.Add(modInfo);
                
                // Реєструємо модифікатор у контролері деки, щоб він міг списувати очки
                if (_deckController != null)
                {
                    var dragHandler = modObj.GetComponent<ModDragHandler>();
                    _deckController.RegisterMod(dragHandler);
                }
            }
        }

        // ===== MOVEMENT CARDS =====
        // 3. Генеруємо та перевіряємо індекси карток ходьби
        if (settings != null && (settings.MovementPoolIndices == null || settings.MovementPoolIndices.Length == 0))
        {
            settings.MovementPoolIndices = _library.GetShuffledMovementIndices();
            // Debug.Log($"[Initializer] Generated MovementPoolIndices: {settings.MovementPoolIndices.Length} cards.");
        }

        // 4. Ініціалізуємо обидва контейнери
        // Ми завжди оновлюємо їх (особливо для Hotseat), щоб Local/Enemy слоти були актуальні
        _movementCardsInstances.Clear();
        int enemyIndex = (playerIndex == 1) ? 2 : 1;

        List<GameObject> p1Cards = InitializeMovementContainer(_movementCardContainer, playerIndex, settings);
        List<GameObject> p2Cards = InitializeMovementContainer(_movementCardContainerEnemy, enemyIndex, settings);

        if (settings != null)
        {
            settings.RegisterMovementCards(playerIndex, p1Cards);
            settings.RegisterMovementCards(enemyIndex, p2Cards);
        }

        // ВИМИКАЄМО ЛЕАЙУТИ ПІСЛЯ ПАУЗИ
        StartCoroutine(DisableLayoutsRoutine());
    }

    // NEW: Initialize deck with specific selected cards (for Hotseat Map transition)
    public void InitializeMapSetupForPlayer(int playerIndex, List<CharacterData> chars, List<ModData> mods, bool clearContainers = true)
    {
        if (_cardContainer != null) _cardContainer.enabled = true;
        if (_modContainer != null) _modContainer.enabled = true;

        if (clearContainers)
        {
            if (_deckController != null) _deckController.ResetController();
            ClearContainer(_cardContainer.transform);
            ClearContainer(_modContainer.transform);
            _cardsInstances.Clear();
            _modsInstances.Clear();
        }

        GameSceneState sceneState = FindObjectOfType<GameSceneState>();

        // Spawn Characters
        for (int i = 0; i < chars.Count; i++)
        {
            GameObject cardObj = Instantiate(_cardPrefab, _cardContainer.transform);
            CardInfo cardInfo = cardObj.GetComponent<CardInfo>();
            if (cardInfo != null)
            {
                cardInfo.CharData = chars[i];
                cardInfo.Initialize();
                _cardsInstances.Add(cardInfo);

                if (_deckController != null)
                {
                    var dragHandler = cardObj.GetComponent<CardDragHandler>();
                    dragHandler.CardID = i;
                    dragHandler.OwnerID = playerIndex;
                    dragHandler.PairID = i / 2;
                    dragHandler.SetDependencies(sceneState, _deckController);
                    _deckController.RegisterCard(dragHandler);
                }
            }
        }

        // Spawn Mods
        for (int i = 0; i < mods.Count; i++)
        {
            GameObject modObj = Instantiate(_modPrefab, _modContainer.transform);
            ModInfo modInfo = modObj.GetComponent<ModInfo>();
            if (modInfo != null)
            {
                modInfo.ModData = mods[i];
                modInfo.Initialize();
                _modsInstances.Add(modInfo);

                if (_deckController != null)
                {
                    var dragHandler = modObj.GetComponent<ModDragHandler>();
                    dragHandler.OwnerID = playerIndex;
                    _deckController.RegisterMod(dragHandler);
                }
            }
        }

        // ВАЖЛИВО: Активуємо всі картки і закидаємо їх у слоти
        foreach (var card in _cardsInstances)
        {
            if (card == null) continue;
            card.gameObject.SetActive(true);
            var handler = card.GetComponent<CardDragHandler>();
            if (handler != null)
            {
                handler.InitializeHome();
            }
        }

        foreach (var mod in _modsInstances)
        {
            if (mod == null) continue;
            mod.gameObject.SetActive(true);
            var handler = mod.GetComponent<ModDragHandler>();
            if (handler != null)
            {
                handler.InitializeHome();
            }
        }

        if (_deckController != null)
        {
            _deckController.AutoFillSlots();
        }

        // REDO: Refresh movement cards so the active player always has their cards at the bottom (Local) 
        // and the correct OwnerIDs are assigned.
        GameSettings settings = sceneState != null ? sceneState._currentSettings : null;
        if (settings != null)
        {
            int enemyIndex = (playerIndex == 1) ? 2 : 1;
            InitializeMovementContainer(_movementCardContainer, playerIndex, settings);
            InitializeMovementContainer(_movementCardContainerEnemy, enemyIndex, settings);
        }

        StartCoroutine(DisableLayoutsRoutine());
    }

    private List<GameObject> InitializeMovementContainer(LayoutGroup container, int playerIdx, GameSettings settings)
    {
        List<GameObject> spawnedCards = new List<GameObject>();
        if (container == null) 
        {
            // Debug.LogWarning($"[Initializer] Container for player {playerIdx} is NULL!");
            return spawnedCards;
        }
        
        container.gameObject.SetActive(true); // Примусово вмикаємо об'єкт
        container.enabled = true;
        ClearContainer(container.transform);

        int[] moveIndices = (settings != null) ? settings.GetMovementIndicesForPlayer(playerIdx) : null;
        List<MovementCard> cards = new List<MovementCard>();

        // PERSISTENCE FIX: Check if we have a hand saved in the snapshot
        PlayerSnapshot snapshot = (settings != null) ? settings.GetSnapshot(playerIdx) : null;
        if (snapshot != null && snapshot.SelectedMovementCards.Count > 0)
        {
            cards = new List<MovementCard>(snapshot.SelectedMovementCards); // COPY to avoid Clearing the source
        }
        else
        {
            // Initial generation
            if (moveIndices != null && moveIndices.Length > 0)
            {
                cards = _library.GetMovementCardsFromIndices(moveIndices);
            }
            else
            {
                cards = _library.GetRandomMovementCards(_movementCardsToSpawn);
            }
            
            // Save to hand for persistence
            if (snapshot != null)
            {
                snapshot.SelectedMovementCards.Clear();
                snapshot.SelectedMovementCards.AddRange(cards);
            }
        }

        for (int i = 0; i < cards.Count; i++)
        {
            GameObject mcObj = Instantiate(_movementCardPrefab, container.transform);
            MovementCardInfo mcInfo = mcObj.GetComponent<MovementCardInfo>();
            if (mcInfo != null)
            {
                mcInfo.MoveCard = cards[i];
                mcInfo.Initialize(playerIdx);
                
                _movementCardsInstances.Add(mcInfo);
                spawnedCards.Add(mcObj);
            }
        }
        return spawnedCards;
    }

    private IEnumerator DisableLayoutsRoutine()
    {
        // Якщо контейнер вимкнений, леайут-група не прораховується.
        // На мить вмикаємо його, щоб Unity встигла розставити елементи.
        // FIX FLICKER: ховаємо панель через CanvasGroup на один кадр, щоб не блимало.
        bool modsOriginallyActive = _modContainer.gameObject.activeSelf;
        CanvasGroup modCg = null;
        float originalAlpha = 1f;

        if (!modsOriginallyActive)
        {
            modCg = _modContainer.GetComponent<CanvasGroup>();
            if (modCg == null) modCg = _modContainer.gameObject.AddComponent<CanvasGroup>();
            
            originalAlpha = modCg.alpha;
            modCg.alpha = 0f;

            _modContainer.gameObject.SetActive(true);
        }
        
        // Форсуємо прорахунок тільки для карт персонажів та модів
        LayoutRebuilder.ForceRebuildLayoutImmediate(_cardContainer.transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_modContainer.transform as RectTransform);

        yield return new WaitForEndOfFrame();

        if (_cardContainer != null) _cardContainer.enabled = false;
        if (_modContainer != null) _modContainer.enabled = false;
        
        if (!modsOriginallyActive) 
        {
            _modContainer.gameObject.SetActive(false);
            if (modCg != null) modCg.alpha = originalAlpha; // Відновлюємо для майбутніх відкриттів
        }

        // Тепер фіксуємо домашні позиції
        foreach (var card in _cardsInstances)
        {
            if (card == null) continue;
            var handler = card.GetComponent<CardDragHandler>();
            if (handler != null) handler.InitializeHome();
        }

        foreach (var mod in _modsInstances)
        {
            if (mod == null) continue;
            var handler = mod.GetComponent<ModDragHandler>();
            if (handler != null) handler.InitializeHome();
        }

        foreach (var mc in _movementCardsInstances)
        {
            if (mc == null) continue;
            var handler = mc.GetComponent<CardDragHandler>();
            if (handler != null) handler.InitializeHome();
            var modHandler = mc.GetComponent<ModDragHandler>();
            if (modHandler != null) modHandler.InitializeHome();
        }
        
        // Debug.Log("[Initializer] Layouts updated and home positions captured.");
    }

    public void CleanUpContainers()
    {
        if (_cardContainer != null) ClearContainer(_cardContainer.transform);
        if (_modContainer != null) ClearContainer(_modContainer.transform);
        // Картки ходьби НЕ ЧИСТИМО, вони назавжди
        _cardsInstances.Clear();
        _modsInstances.Clear();
    }

    private void ClearContainer(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
        container.DetachChildren();
    }

    public void RefreshBattleUI()
    {
        if (InitiativeSystem.Instance == null) return;
        int currentActivePlayer = InitiativeSystem.Instance.CurrentTurnPlayerID;
        int enemyPlayer = (currentActivePlayer == 1) ? 2 : 1;

        GameSceneState sceneState = FindObjectOfType<GameSceneState>();
        GameSettings settings = sceneState != null ? sceneState._currentSettings : null;
        
        if (settings is HotseatSettings hSettings)
        {
            ApplyHotseatHand(currentActivePlayer);
        }

        // 1. Refresh MOVEMENT cards (Always show Active Player at bottom, Enemy at top)
        _movementCardsInstances.Clear();
        InitializeMovementContainer(_movementCardContainer, currentActivePlayer, settings);
        InitializeMovementContainer(_movementCardContainerEnemy, enemyPlayer, settings);

        // Force layout rebuild and grab homes
        StartCoroutine(RebuildMovementCardsRoutine());
    }

    public void ApplyHotseatHand(int playerID)
    {
        if (_deckController == null) return;

        // 1. Reset controller (hides previous and clears lists)
        _deckController.ResetController();

        // 2. Filter and Register ALL cards for THIS player
        foreach (var cardInfo in _cardsInstances)
        {
            if (cardInfo == null) continue;
            var drag = cardInfo.GetComponent<CardDragHandler>();
            if (drag != null && drag.OwnerID == playerID)
            {
                cardInfo.gameObject.SetActive(true);
                _deckController.RegisterCard(drag);
            }
            else
            {
                cardInfo.gameObject.SetActive(false);
            }
        }

        // 3. Filter and Register Mods for THIS player
        foreach (var modInfo in _modsInstances)
        {
            if (modInfo == null) continue;
            var drag = modInfo.GetComponent<ModDragHandler>();
            
            // Logic: if it's attached to a card, it follows card visibility.
            // If it's in the mod container, we check its OwnerID (re-added).
            if (drag != null && modInfo.transform.parent == _modContainer.transform)
            {
                bool isMe = drag.OwnerID == playerID;
                modInfo.gameObject.SetActive(isMe);
                if (isMe) _deckController.RegisterMod(drag);
            }
        }

        // 4. Fill slots
        _deckController.AutoFillSlots();

        // 5. Special Sync: if map is finalized, make sure the cards currently on map are "ACTIVE" in slots
        if (InitiativeSystem.Instance != null && InitiativeSystem.Instance.IsFinalized && CharacterPlacementManager.Instance != null)
        {
             for (int pairID = 0; pairID < 5; pairID++)
             {
                 int mapLibIdx = CharacterPlacementManager.Instance.GetSpawnedCharacterLibIndex(playerID, pairID);
                 if (mapLibIdx == -1) continue;

                 // Find the cards in that pair in controller
                 // NO NEED: Controller already has them in _cards. Let's just find the one that matches map
                 var pCards = _cardsInstances.FindAll(c => {
                     var d = c.GetComponent<CardDragHandler>();
                     return d != null && d.OwnerID == playerID && d.PairID == pairID;
                 });

                 var activeInPair = pCards.Find(c => CharacterPlacementManager.Instance.GetLibraryIndex(c.CharData) == mapLibIdx);
                 if (activeInPair != null)
                 {
                     var drag = activeInPair.GetComponent<CardDragHandler>();
                     if (drag != null) _deckController.MakeActive(drag);
                 }
             }
        }
    }

    private IEnumerator RebuildMovementCardsRoutine()
    {
        if (_movementCardContainer != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_movementCardContainer.transform as RectTransform);
        if (_movementCardContainerEnemy != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_movementCardContainerEnemy.transform as RectTransform);

        yield return new WaitForEndOfFrame();

        foreach (var mc in _movementCardsInstances)
        {
            if (mc == null) continue;
            var handler = mc.GetComponent<CardDragHandler>();
            if (handler != null) handler.InitializeHome();
            var modHandler = mc.GetComponent<ModDragHandler>();
            if (modHandler != null) modHandler.InitializeHome();
        }
    }
    private int GetPlayerIndex(SceneState mode, GameSettings settings)
    {
        if (settings != null)
        {
            return settings.CurrentPlayerIndex;
        }

        switch (mode)
        {
            case SceneState.Multiplayer:
                return PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber : 1;
            default:
                return 1;
        }
    }
}