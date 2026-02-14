using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using System.Collections;

public class GameDataInitializer : MonoBehaviour
{
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

        // 4. Ініціалізуємо обидва контейнери ТІЛЬКИ ЯКЩО ВОНИ ПУСТІ
        if ((_movementCardContainer != null && _movementCardContainer.transform.childCount == 0) && 
            (_movementCardContainerEnemy != null && _movementCardContainerEnemy.transform.childCount == 0))
        {
            _movementCardsInstances.Clear();
            int enemyIndex = (playerIndex == 0) ? 1 : 0;

            List<GameObject> p1Cards = InitializeMovementContainer(_movementCardContainer, playerIndex, settings);
            List<GameObject> p2Cards = InitializeMovementContainer(_movementCardContainerEnemy, enemyIndex, settings);

            if (settings != null)
            {
                settings.RegisterMovementCards(playerIndex, p1Cards);
                settings.RegisterMovementCards(enemyIndex, p2Cards);
            }
        }

        // ВИМИКАЄМО ЛЕАЙУТИ ПІСЛЯ ПАУЗИ
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
        List<MovementCard> cards;

        if (moveIndices != null && moveIndices.Length > 0)
        {
            cards = _library.GetMovementCardsFromIndices(moveIndices);
        }
        else
        {
            cards = _library.GetRandomMovementCards(_movementCardsToSpawn);
        }

        for (int i = 0; i < _movementCardsToSpawn; i++)
        {
            GameObject mcObj = Instantiate(_movementCardPrefab, container.transform);
            MovementCardInfo mcInfo = mcObj.GetComponent<MovementCardInfo>();
            if (mcInfo != null)
            {
                if (i < cards.Count) mcInfo.MoveCard = cards[i];
                mcInfo.Initialize();
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
        bool modsOriginallyActive = _modContainer.gameObject.activeSelf;
        if (!modsOriginallyActive) _modContainer.gameObject.SetActive(true);
        
        // Форсуємо прорахунок тільки для карт персонажів та модів
        LayoutRebuilder.ForceRebuildLayoutImmediate(_cardContainer.transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_modContainer.transform as RectTransform);

        yield return new WaitForEndOfFrame();

        if (_cardContainer != null) _cardContainer.enabled = false;
        if (_modContainer != null) _modContainer.enabled = false;
        
        if (!modsOriginallyActive) _modContainer.gameObject.SetActive(false);

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
                return PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber - 1 : 0;
            default:
                return 0;
        }
    }
}