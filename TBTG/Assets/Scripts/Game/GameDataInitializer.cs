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
        if (_movementCardContainer != null) _movementCardContainer.enabled = true;
        //ClearContainer(_movementCardContainer.transform);
        //_movementCardsInstances.Clear();
        List<MovementCard> randomMoveCards = _library.GetRandomMovementCards(_movementCardsToSpawn);
        
        for (int i = 0; i < _movementCardsToSpawn; i++)
        {
            GameObject mcObj = Instantiate(_movementCardPrefab, _movementCardContainer.transform);
            MovementCardInfo mcInfo = mcObj.GetComponent<MovementCardInfo>();
            if (mcInfo != null)
            {
                if (i < randomMoveCards.Count) mcInfo.MoveCard = randomMoveCards[i];
                mcInfo.Initialize();
                _movementCardsInstances.Add(mcInfo);
            }
        }

        // ВИМИКАЄМО ЛЕАЙУТИ СИНХРОННО
        DisableLayouts();

        // Debug.Log("Game Data Initialized SUCCESS");
    }

    private void DisableLayouts()
    {
        // Якщо контейнер вимкнений, леайут-група не прораховується.
        // На мить вмикаємо його, щоб Unity встигла розставити елементи.
        bool modsOriginallyActive = _modContainer.gameObject.activeSelf;
        if (!modsOriginallyActive) _modContainer.gameObject.SetActive(true);
        
        bool movesOriginallyActive = _movementCardContainer != null && _movementCardContainer.gameObject.activeSelf;
        if (_movementCardContainer != null && !movesOriginallyActive) _movementCardContainer.gameObject.SetActive(true);

        // Форсуємо прорахунок, щоб позиції точно були вірні
        LayoutRebuilder.ForceRebuildLayoutImmediate(_cardContainer.transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_modContainer.transform as RectTransform);
        if (_movementCardContainer != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_movementCardContainer.transform as RectTransform);

        if (_cardContainer != null) _cardContainer.enabled = false;
        if (_modContainer != null) _modContainer.enabled = false;
        if (_movementCardContainer != null) _movementCardContainer.enabled = false;

        // Повертаємо стан активності контейнера модів та карток назад
        if (!modsOriginallyActive) _modContainer.gameObject.SetActive(false);
        if (_movementCardContainer != null && !movesOriginallyActive) _movementCardContainer.gameObject.SetActive(false);

        // Тепер фіксуємо домашні позиції
        foreach (var card in _cardsInstances)
        {
            var handler = card.GetComponent<CardDragHandler>();
            if (handler != null) handler.InitializeHome();
        }

        foreach (var mod in _modsInstances)
        {
            var handler = mod.GetComponent<ModDragHandler>();
            if (handler != null) handler.InitializeHome();
        }
        
        // For movement cards, we haven't implemented drag handler yet, but if they have one:
        // foreach (var mc in _movementCardsInstances) { ... }

        // Debug.Log("Layout Groups DISABLED and Home Positions FIXED");
    }

    public void CleanUpContainers()
    {
        if (_cardContainer != null) ClearContainer(_cardContainer.transform);
        if (_modContainer != null) ClearContainer(_modContainer.transform);
        //if (_movementCardContainer != null) ClearContainer(_movementCardContainer.transform);
        _cardsInstances.Clear();
        _modsInstances.Clear();
        //_movementCardsInstances.Clear();
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