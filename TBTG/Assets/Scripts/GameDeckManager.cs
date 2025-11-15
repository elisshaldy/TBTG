using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameDeckManager : MonoBehaviour
{
    [Header("Deck Data")]
    [Tooltip("Головна колода (пул) усіх доступних карт.")]
    public MasterDeckData MasterDeck;

    [Tooltip("Рука гравця 1, куди ми зберігаємо обрані карти.")]
    public PlayerHandData Player1Hand;

    [Tooltip("Рука гравця 2, куди ми зберігаємо обрані карти.")]
    public PlayerHandData Player2Hand;

    [Header("Draft UI")]
    public GameObject ConfirmSelectionButton;
    public int CardsToSelect = 8;
    public int CardsToShow = 10;

    [Header("UI References")]
    [Tooltip("RawImage що має бути вимкнено під час драфту")]
    public RawImage GameFieldRawImage;

    [Tooltip("CanvasGroup для контролю інтерактивності всієї сцени драфту")]
    public CanvasGroup DraftCanvasGroup;

    [Header("Drag & Drop References")]
    public PairFormationManager PairFormationManager;

    [Header("Manager References")]
    public PlayerCardManager CardManager;
    [Tooltip("Менеджер, який обробляє наступну фазу - купівлю рис.")]
    public TraitPurchaseManager TraitPurchaseManager;

    private GameManager _gameManager;
    private List<CardSelectionHandler> _activeDraftCards = new List<CardSelectionHandler>();
    private List<(CharacterData data, CardSelectionHandler handler, SelectionMode mode)> _selectedCardsInfo =
        new List<(CharacterData data, CardSelectionHandler handler, SelectionMode mode)>();
    private List<CharacterData> _selectedCharacters = new List<CharacterData>();
    private bool _isDraftPhaseActive = false;
    private PlayerHandData _activePlayerHand;
    private int _activePlayerID = 1;

    void Awake()
    {
        // Переконайтеся, що всі необхідні посилання встановлені
        Assert.IsNotNull(MasterDeck, "MasterDeckData не призначено в GameDeckManager.");
        Assert.IsNotNull(Player1Hand, "Player1HandData не призначено в GameDeckManager.");
        Assert.IsNotNull(Player2Hand, "Player2HandData не призначено в GameDeckManager.");
        Assert.IsNotNull(CardManager, "CardManager не призначено в GameDeckManager.");
        Assert.IsNotNull(TraitPurchaseManager, "TraitPurchaseManager не призначено в GameDeckManager.");
        Assert.IsNotNull(PairFormationManager, "PairFormationManager не призначено в GameDeckManager.");

        // Знаходимо GameManager, якщо він не призначений
        if (_gameManager == null) _gameManager = FindObjectOfType<GameManager>();

        if (ConfirmSelectionButton != null) ConfirmSelectionButton.SetActive(false);

        // Спочатку вимикаємо RawImage
        SetGameFieldRawImageActive(false);
    }

    void Start()
    {
        // Починаємо драфт для першого гравця.
        StartDraftPhase();
    }

    /// <summary>
    /// Контролює активність RawImage та інших UI елементів
    /// </summary>
    private void SetGameFieldRawImageActive(bool isActive)
    {
        if (GameFieldRawImage != null)
        {
            GameFieldRawImage.gameObject.SetActive(isActive);
            Debug.Log($"GameFieldRawImage активність: {isActive}");
        }

        // Додатково контролюємо інтерактивність всієї сцени драфту
        if (DraftCanvasGroup != null)
        {
            DraftCanvasGroup.interactable = isActive;
            DraftCanvasGroup.blocksRaycasts = isActive;
        }
    }

    // ----------------------------------------------------------------------
    // ФАЗА ДРАФТУ З DRAG & DROP
    // ----------------------------------------------------------------------

    public void StartDraftPhase()
    {
        // ВИМИКАЄМО RawImage на початку драфту
        SetGameFieldRawImageActive(false);

        // Скидаємо систему пар
        PairFormationManager.ResetAllPairs();

        // 1. Увімкнення батьківського контейнера, щоб відобразити картки
        if (CardManager != null && CardManager.CardsContainer != null)
        {
            CardManager.CardsContainer.gameObject.SetActive(true);
        }

        // Визначаємо, чий хід зараз (P1 або P2)
        PlayerHandData opponentHand = (_activePlayerID == 1) ? Player2Hand : Player1Hand;
        _activePlayerHand = (_activePlayerID == 1) ? Player1Hand : Player2Hand;

        // Очищаємо тимчасові списки вибору
        _selectedCharacters.Clear();
        _selectedCardsInfo.Clear();
        _isDraftPhaseActive = true;

        _activePlayerHand.ClearHand();
        Debug.Log($"P{_activePlayerID}: Starting Draft Phase. Hand cleared.");

        // 1. Визначаємо пул карт для показу
        List<CharacterData> availablePool = MasterDeck.AllAvailableCharacters
            .Except(Player1Hand.SelectedCharacters)
            .Except(Player2Hand.SelectedCharacters)
            .Except(Player1Hand.DiscardedCharacters)
            .Except(Player2Hand.DiscardedCharacters)
            .ToList();

        // 2. Рандомізуємо та обираємо CardsToShow карт
        List<CharacterData> draftPool = availablePool
            .OrderBy(x => Random.value)
            .Take(CardsToShow)
            .ToList();

        // 3. Створюємо картки на сцені
        _activeDraftCards.Clear();
        _activeDraftCards = CardManager.LoadDraftCards(draftPool);

        // 4. Ініціалізуємо drag & drop систему
        InitializeDragDropSystem(_activeDraftCards);

        UpdateConfirmButtonState();

        Debug.Log($"Драфт фаза для гравця {_activePlayerID} розпочата. Drag & Drop активний.");
    }

    /// <summary>
    /// Ініціалізує drag & drop систему для карток драфту
    /// </summary>
    private void InitializeDragDropSystem(List<CardSelectionHandler> draftHandlers)
    {
        foreach (var cardHandler in draftHandlers)
        {
            // Підписка на події драг & дроп
            cardHandler.OnCardBeginDrag += PairFormationManager.HandleCardBeginDrag;
            cardHandler.OnCardEndDrag += PairFormationManager.HandleCardEndDrag;
            cardHandler.OnCardDropped += PairFormationManager.HandleCardDropped;
            cardHandler.OnCardReturnedToDraft += PairFormationManager.HandleCardReturnedToDraft;
        }

        // Увімкнути драг & дроп
        PairFormationManager.SetDragDropEnabled(true);

        // Підписка на події завершення пар
        // Можна додати через UnityEvent в PairFormationManager
    }

    /// <summary>
    /// Оновлює стан кнопки підтвердження на основі завершених пар
    /// </summary>
    private void UpdateConfirmButtonState()
    {
        if (ConfirmSelectionButton != null)
        {
            bool allPairsComplete = PairFormationManager.GetCompletedPairsCount() == 4;
            ConfirmSelectionButton.SetActive(allPairsComplete);

            if (allPairsComplete)
            {
                Debug.Log("All 4 pairs completed! Confirm button activated.");
            }
        }
    }

    /// <summary>
    /// Викликається при зміні стану пар (можна підписатися через UnityEvent)
    /// </summary>
    public void OnPairsUpdated()
    {
        UpdateConfirmButtonState();

        // Оновлюємо список обраних карток з пар
        UpdateSelectedCharactersFromPairs();
    }

    /// <summary>
    /// Оновлює список обраних карток на основі сформованих пар
    /// </summary>
    private void UpdateSelectedCharactersFromPairs()
    {
        _selectedCharacters.Clear();
        _selectedCardsInfo.Clear();

        var formedPairs = PairFormationManager.GetFormedPairs();
        foreach (var pair in formedPairs)
        {
            if (pair.ActiveCharacter != null)
            {
                _selectedCharacters.Add(pair.ActiveCharacter);
                // Знаходимо handler для активної картки
                var activeHandler = _activeDraftCards.FirstOrDefault(h => h.CardData == pair.ActiveCharacter);
                if (activeHandler != null)
                {
                    _selectedCardsInfo.Add((pair.ActiveCharacter, activeHandler, SelectionMode.Visible));
                }
            }

            if (pair.HiddenCharacter != null)
            {
                _selectedCharacters.Add(pair.HiddenCharacter);
                // Знаходимо handler для прихованої картки
                var hiddenHandler = _activeDraftCards.FirstOrDefault(h => h.CardData == pair.HiddenCharacter);
                if (hiddenHandler != null)
                {
                    _selectedCardsInfo.Add((pair.HiddenCharacter, hiddenHandler, SelectionMode.Hidden));
                }
            }
        }

        Debug.Log($"Updated selected characters: {_selectedCharacters.Count} cards from {formedPairs.Count} pairs");
    }

    /// <summary>
    /// Викликається при натисканні кнопки "Підтвердити Вибір".
    /// </summary>
    public void ConfirmSelection()
    {
        if (PairFormationManager.GetCompletedPairsCount() != 4)
        {
            Debug.LogWarning("Не всі пари сформовані! Потрібно 4 повних пар.");
            return;
        }

        _isDraftPhaseActive = false;

        // Фіналізуємо драфт фазу
        FinalizeDraftPhase();

        if (_activePlayerID == 1)
        {
            // Переходимо до P2
            _activePlayerID = 2;
            Debug.Log("Draft P1 Completed. Initiating Draft for Player 2.");

            // ЗАЛИШАЄМО RawImage ВИМКНЕНИМ для другого гравця
            StartDraftPhase();
        }
        else
        {
            // Обидва гравці завершили драфт
            Debug.Log("Draft Phase Completed for both players.");

            // УСІ ГРАВЦІ ЗАВЕРШИЛИ ДРАФТ - УВІМКНУТИ RAWIMAGE
            SetGameFieldRawImageActive(true);

            // 5. Починаємо фазу купівлі рис (Trait Purchase)
            StartTraitPurchasePhase();
        }
    }

    /// <summary>
    /// Фіналізує драфт фазу та готує дані для наступної фази
    /// </summary>
    private void FinalizeDraftPhase()
    {
        // Вимкнути драг & дроп
        PairFormationManager.SetDragDropEnabled(false);

        // Отримати сформовані пари
        var formedPairs = PairFormationManager.GetFormedPairs();

        // Оновлюємо список обраних карток
        UpdateSelectedCharactersFromPairs();

        // --- ЛОГІКА: Визначення невибраних карт (які підуть у відкинуті) ---
        List<CharacterData> unselectedCharacters =
            _activeDraftCards
                .Select(csh => csh.CardData)
                .Except(_selectedCharacters)
                .ToList();
        // ------------------------------------------

        // 1. Зберігаємо обрані карти в активну руку
        _activePlayerHand.SelectedCharacters.Clear();
        _activePlayerHand.SelectedCharacters.AddRange(_selectedCharacters);

        // 2. Зберігаємо невибрані карти (які більше не будуть доступні)
        _activePlayerHand.DiscardedCharacters.Clear();
        _activePlayerHand.DiscardedCharacters.AddRange(unselectedCharacters);

        Debug.Log($"P{_activePlayerID}: Draft confirmed. {_selectedCharacters.Count} selected, {unselectedCharacters.Count} discarded.");

        // !!! ЗБЕРІГАННЯ РЕЖИМІВ ВИБОРУ (Visible/Hidden) !!!
        _activePlayerHand.SetSelectionModes(_selectedCardsInfo.Select(i => (i.data, i.mode)).ToList());
        // ------------------------------------------

        // 3. Очищаємо сцену та UI елементи
        CardManager.ClearCards();
        if (CardManager.CardsContainer != null)
        {
            CardManager.CardsContainer.gameObject.SetActive(false);
        }

        if (ConfirmSelectionButton != null) ConfirmSelectionButton.SetActive(false);

        Debug.Log($"Draft phase for player {_activePlayerID} finalized successfully.");
    }

    // ----------------------------------------------------------------------
    // ФАЗА ПОКУПКИ РИС
    // ----------------------------------------------------------------------

    private void StartTraitPurchasePhase()
    {
        Debug.Log("Draft Phase Completed. Starting Trait Purchase Phase. RawImage активний.");

        if (TraitPurchaseManager != null)
        {
            // Передаємо обидві руки менеджеру для початку фази
            TraitPurchaseManager.StartPurchasePhase(Player1Hand, Player2Hand);
        }
        else
        {
            Debug.LogError("TraitPurchaseManager не призначено. Неможливо перейти до наступної фази.");
        }
    }

    // ----------------------------------------------------------------------
    // ДОДАТКОВІ ПУБЛІЧНІ МЕТОДИ ДЛЯ КОНТРОЛЮ
    // ----------------------------------------------------------------------

    /// <summary>
    /// Публічний метод для примусового вмикання/вимикання RawImage
    /// </summary>
    public void SetRawImageActive(bool active)
    {
        SetGameFieldRawImageActive(active);
    }

    /// <summary>
    /// Перевірити чи завершено драфт фазу
    /// </summary>
    public bool IsDraftPhaseComplete()
    {
        return _activePlayerID == 2 && !_isDraftPhaseActive;
    }

    /// <summary>
    /// Отримати поточний прогрес драфту
    /// </summary>
    public (int currentPlayer, int completedPairs, int totalPairs) GetDraftProgress()
    {
        return (_activePlayerID, PairFormationManager.GetCompletedPairsCount(), 4);
    }

    /// <summary>
    /// Скинути драфт систему (для рестарту гри)
    /// </summary>
    public void ResetDraftSystem()
    {
        _activePlayerID = 1;
        _isDraftPhaseActive = false;
        _selectedCharacters.Clear();
        _selectedCardsInfo.Clear();
        _activeDraftCards.Clear();

        // Очищаємо руки гравців
        Player1Hand.ClearHand();
        Player2Hand.ClearHand();

        // Скидаємо систему пар
        PairFormationManager.ResetAllPairs();

        // Вимикаємо RawImage при рестарті
        SetGameFieldRawImageActive(false);

        Debug.Log("Draft system reset complete.");
    }

    /// <summary>
    /// Примусово завершити драфт поточного гравця (для тестування)
    /// </summary>
    public void ForceCompleteDraft()
    {
        if (_isDraftPhaseActive)
        {
            Debug.Log("Forcing draft completion...");
            ConfirmSelection();
        }
    }
}