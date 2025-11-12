using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.UI;

// Цей менеджер відповідає за початковий етап ДРАФТУ (вибору) карт персонажів.
public class GameDeckManager : MonoBehaviour
{
    // *** ПОЛЕ ВИПРАВЛЕНО: ТИП З CharacterData ЗМІНЕНО НА MasterDeckData ***
    [Header("Deck Data")]
    [Tooltip("Головна колода (пул) усіх доступних карт.")]
    public MasterDeckData MasterDeck;

    // !!! НОВЕ: Для Hot-Seat нам потрібні окремі посилання на руки гравців !!!
    [Tooltip("Рука гравця 1, куди ми зберігаємо обрані карти.")]
    public PlayerHandData Player1Hand;

    [Tooltip("Рука гравця 2, куди ми зберігаємо обрані карти.")]
    public PlayerHandData Player2Hand;
    // ------------------------------------------------------------------

    [Header("Draft UI")]
    [Tooltip("Кнопка, яка підтверджує вибір 8 карт.")]
    public GameObject ConfirmSelectionButton;

    [Tooltip("Необхідна кількість карт, яку гравець має обрати.")]
    public int CardsToSelect = 8;

    [Tooltip("Кількість карт, які будуть показані для вибору.")]
    public int CardsToShow = 10;
    // -------------------------------------------------------------------


    [Header("Manager References")]
    public PlayerCardManager CardManager;

    private GameManager _gameManager;

    // Тимчасовий список усіх 10 карт, які зараз відображаються для вибору.
    private List<CardSelectionHandler> _activeDraftCards = new List<CardSelectionHandler>();

    // Список обраних карток
    private List<CharacterData> _selectedCharacters = new List<CharacterData>();

    private bool _isDraftPhaseActive = false;

    // !!! НОВЕ: Поточна активна рука гравця (для Hot-Seat) !!!
    private PlayerHandData _activePlayerHand;
    // !!! НОВЕ: Поточний ID гравця (для Hot-Seat) !!!
    private int _activePlayerID = 1;


    void Awake()
    {
        // Переконайтеся, що всі необхідні посилання встановлені
        Assert.IsNotNull(MasterDeck, "MasterDeckData не призначено в GameDeckManager.");
        Assert.IsNotNull(Player1Hand, "Player1HandData не призначено в GameDeckManager.");
        Assert.IsNotNull(Player2Hand, "Player2HandData не призначено в GameDeckManager.");
        Assert.IsNotNull(CardManager, "CardManager не призначено в GameDeckManager.");

        // Знаходимо GameManager, якщо він не призначений
        if (_gameManager == null) _gameManager = FindObjectOfType<GameManager>();

        if (ConfirmSelectionButton != null) ConfirmSelectionButton.SetActive(false);
    }

    void Start()
    {
        // Припускаємо, що GameManager ініціалізує PlayerHand'и.
        // Починаємо драфт для першого гравця.
        StartDraftPhase();
    }


    // ----------------------------------------------------------------------
    // ФАЗА ДРАФТУ
    // ----------------------------------------------------------------------

    public void StartDraftPhase()
    {
        // Визначаємо, чий хід зараз (P1 або P2)
        _activePlayerHand = (_activePlayerID == 1) ? Player1Hand : Player2Hand;

        // Очищаємо тимчасовий список вибору
        _selectedCharacters.Clear();
        _isDraftPhaseActive = true;

        // !!! ВИПРАВЛЕННЯ CS1061 (Line 78) - Повне очищення руки гравця !!!
        // Очищаємо активну руку перед початком кожного драфту (вибрані + відкинуті)
        _activePlayerHand.ClearHand();
        Debug.Log($"P{_activePlayerID}: Starting Draft Phase. Hand cleared.");

        // 1. Визначаємо пул карт для показу
        // Враховуємо карти, які вже були обрані іншим гравцем (для P2)
        List<CharacterData> availablePool = MasterDeck.AllAvailableCharacters
            .Except(Player1Hand.SelectedCharacters) // Виключаємо обрані P1 (для P2)
            .Except(Player2Hand.SelectedCharacters) // Виключаємо обрані P2 (для P1 - тут поки що нічого, але корисно)
            .ToList();

        // 2. Рандомізуємо та обираємо CardsToShow карт
        List<CharacterData> draftPool = availablePool
            .OrderBy(x => Random.value)
            .Take(CardsToShow) // Беремо 10 карт
            .ToList();

        // 3. Створюємо картки на сцені
        _activeDraftCards.Clear();

        // !!! ВИКЛИК ВИПРАВЛЕНОГО МЕТОДУ LoadDraftCards !!!
        _activeDraftCards = CardManager.LoadDraftCards(draftPool);

        // 4. Призначаємо обробники кліків
        foreach (var cardHandler in _activeDraftCards)
        {
            cardHandler.OnCardClicked += HandleCardSelection;
        }

        UpdateConfirmButtonState();
    }


    /// <summary>
    /// Обробляє клік по картці.
    /// </summary>
    private void HandleCardSelection(CardSelectionHandler handler)
    {
        if (!_isDraftPhaseActive) return;

        if (handler.IsSelected)
        {
            // Скасування виділення
            handler.SetSelection(false);
            _selectedCharacters.Remove(handler.CardData);
            Debug.Log($"Card {handler.CardData.CharacterName} deselected.");
        }
        else if (_selectedCharacters.Count < CardsToSelect)
        {
            // Виділення
            handler.SetSelection(true);
            _selectedCharacters.Add(handler.CardData);
        }
        else
        {
            Debug.LogWarning($"P{_activePlayerID}: Досягнуто ліміту ({CardsToSelect}) карт.");
        }

        UpdateConfirmButtonState();
    }

    private void UpdateConfirmButtonState()
    {
        if (ConfirmSelectionButton != null)
        {
            // Активуємо кнопку, тільки якщо обрана необхідна кількість
            bool canConfirm = _selectedCharacters.Count == CardsToSelect;
            ConfirmSelectionButton.SetActive(canConfirm);
        }
    }


    /// <summary>
    /// Викликається при натисканні кнопки "Підтвердити Вибір".
    /// </summary>
    public void ConfirmSelection()
    {
        if (_selectedCharacters.Count != CardsToSelect)
        {
            Debug.LogWarning("Кількість обраних карт не відповідає необхідній.");
            return;
        }

        _isDraftPhaseActive = false;

        // --- ЛОГІКА: Визначення невибраних карт (які підуть у відкинуті) ---
        List<CharacterData> unselectedCharacters =
            _activeDraftCards
                .Select(csh => csh.CardData)
                .Except(_selectedCharacters) // Виключаємо обрані карти
                .ToList();
        // ------------------------------------------

        // 1. Зберігаємо обрані карти в активну руку
        _activePlayerHand.SelectedCharacters.Clear();
        _activePlayerHand.SelectedCharacters.AddRange(_selectedCharacters);

        // 2. Зберігаємо невибрані карти (ВИРІШЕННЯ ПОМИЛОК 204 і 205)
        _activePlayerHand.DiscardedCharacters.Clear();
        _activePlayerHand.DiscardedCharacters.AddRange(unselectedCharacters);

        Debug.Log($"P{_activePlayerID}: Draft confirmed. {CardsToSelect} selected, {unselectedCharacters.Count} discarded.");

        // 3. Очищаємо сцену
        CardManager.ClearCards();

        // 4. Приховуємо кнопку
        if (ConfirmSelectionButton != null) ConfirmSelectionButton.SetActive(false);

        // 5. Переходимо до наступного гравця/фази
        if (_activePlayerID == 1)
        {
            // Переходимо до P2
            _activePlayerID = 2;

            // !!! ТУТ ПОВИННА БУТИ ЛОГІКА HANDOVER UI !!!
            // Наприклад: HandoverUI.ShowScreen(2, StartDraftPhase);
            StartDraftPhase(); // Тимчасовий прямий виклик для тестування
        }
        else
        {
            // Обидва гравці завершили драфт
            StartPairingPhase();
        }
    }


    private void StartPairingPhase()
    {
        Debug.Log("Draft Phase Completed. Starting Pairing Phase.");
        // TODO: Додати логіку UI для формування пар
        // ...

        // Після формування пар: _gameManager.StartPlacementPhase();
    }
}