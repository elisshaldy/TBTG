using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Потрібно для PointerEventData (Left/Right Click)

// Цей менеджер відповідає за початковий етап ДРАФТУ (вибору) карт персонажів.
public class GameDeckManager : MonoBehaviour
{
    // ... (поля MasterDeck, Player1Hand, Player2Hand)

    [Header("Deck Data")]
    [Tooltip("Головна колода (пул) усіх доступних карт.")]
    public MasterDeckData MasterDeck;

    [Tooltip("Рука гравця 1, куди ми зберігаємо обрані карти.")]
    public PlayerHandData Player1Hand;

    [Tooltip("Рука гравця 2, куди ми зберігаємо обрані карти.")]
    public PlayerHandData Player2Hand;
    // ------------------------------------------------------------------

    [Header("Draft UI")]
    // ... (поля ConfirmSelectionButton, CardsToSelect, CardsToShow)
    public GameObject ConfirmSelectionButton;
    public int CardsToSelect = 8;
    public int CardsToShow = 10;
    // -------------------------------------------------------------------


    [Header("Manager References")]
    public PlayerCardManager CardManager;
    [Tooltip("Менеджер, який обробляє наступну фазу - купівлю рис.")]
    public TraitPurchaseManager TraitPurchaseManager;

    private GameManager _gameManager;

    // Тимчасовий список усіх 10 карт, які зараз відображаються для вибору.
    private List<CardSelectionHandler> _activeDraftCards = new List<CardSelectionHandler>();

    // !!! ВИПРАВЛЕННЯ CS0426: ЗАМІНЮЄМО CardSelectionHandler.SelectionMode НА SelectionMode !!!
    // Список обраних карток разом із режимом вибору (Visible/Hidden).
    // Тепер використовуємо глобальний тип SelectionMode.
    private List<(CharacterData data, CardSelectionHandler handler, SelectionMode mode)> _selectedCardsInfo =
        new List<(CharacterData data, CardSelectionHandler handler, SelectionMode mode)>();
    // ----------------------------------------------------------------------------------

    // Список обраних карток (тільки Data) - використовується для підрахунку.
    private List<CharacterData> _selectedCharacters = new List<CharacterData>();

    private bool _isDraftPhaseActive = false;

    // Поточна активна рука гравця (для Hot-Seat)
    private PlayerHandData _activePlayerHand;
    // Поточний ID гравця (для Hot-Seat)
    private int _activePlayerID = 1;

    // ... (методи Awake(), Start(), StartDraftPhase(), HandleCardSelection(), 
    //      UpdateConfirmButtonState(), ConfirmSelection(), StartTraitPurchasePhase())

    void Awake()
    {
        // Переконайтеся, що всі необхідні посилання встановлені
        Assert.IsNotNull(MasterDeck, "MasterDeckData не призначено в GameDeckManager.");
        Assert.IsNotNull(Player1Hand, "Player1HandData не призначено в GameDeckManager.");
        Assert.IsNotNull(Player2Hand, "Player2HandData не призначено в GameDeckManager.");
        Assert.IsNotNull(CardManager, "CardManager не призначено в GameDeckManager.");
        // АСЕРТ НОВОГО ПОЛЯ (TraitPurchaseManager)
        Assert.IsNotNull(TraitPurchaseManager, "TraitPurchaseManager не призначено в GameDeckManager.");

        // Знаходимо GameManager, якщо він не призначений
        if (_gameManager == null) _gameManager = FindObjectOfType<GameManager>();

        if (ConfirmSelectionButton != null) ConfirmSelectionButton.SetActive(false);
    }

    void Start()
    {
        // Починаємо драфт для першого гравця.
        StartDraftPhase();
    }


    // ----------------------------------------------------------------------
    // ФАЗА ДРАФТУ
    // ----------------------------------------------------------------------

    public void StartDraftPhase()
    {
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
        _selectedCardsInfo.Clear(); // Очищаємо список з режимами
        _isDraftPhaseActive = true;

        // !!! КОРЕКТУЄМО: Викликаємо ClearHand(), який має бути в PlayerHandData.cs
        _activePlayerHand.ClearHand();
        Debug.Log($"P{_activePlayerID}: Starting Draft Phase. Hand cleared.");

        // 1. Визначаємо пул карт для показу
        // Виключаємо карти, які вже обрані АБО відкинуті обома гравцями
        List<CharacterData> availablePool = MasterDeck.AllAvailableCharacters
            .Except(Player1Hand.SelectedCharacters)
            .Except(Player2Hand.SelectedCharacters)
            .Except(Player1Hand.DiscardedCharacters)
            .Except(Player2Hand.DiscardedCharacters)
            .ToList();

        // 2. Рандомізуємо та обираємо CardsToShow карт
        List<CharacterData> draftPool = availablePool
            .OrderBy(x => Random.value)
            .Take(CardsToShow) // Беремо 10 карт
            .ToList();

        // 3. Створюємо картки на сцені
        _activeDraftCards.Clear();
        _activeDraftCards = CardManager.LoadDraftCards(draftPool);

        // 4. Призначаємо обробники кліків
        foreach (var cardHandler in _activeDraftCards)
        {
            // ПІДПИСКА
            cardHandler.OnCardClicked += HandleCardSelection;
        }

        UpdateConfirmButtonState();
    }


    /// <summary>
    /// Обробляє клік по картці, дозволяючи обрати до CardsToSelect карт і призначити режим (Visible/Hidden)
    /// за допомогою лівого/правого кліку.
    /// </summary>
    private void HandleCardSelection(CardSelectionHandler cardHandler, PointerEventData eventData)
    {
        if (!_isDraftPhaseActive) return;

        CharacterData data = cardHandler.CardData;

        // Використовуємо глобальний SelectionMode
        SelectionMode newMode;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            newMode = SelectionMode.Visible;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            newMode = SelectionMode.Hidden;
        }
        else
        {
            // Ігноруємо інші типи кліків
            return;
        }

        // Шукаємо, чи картка вже обрана
        var existingSelection = _selectedCardsInfo.FirstOrDefault(i => i.data == data);

        if (existingSelection.data != null)
        {
            // 1. Картка вже обрана. Скасовуємо виділення.
            cardHandler.SetSelection(SelectionMode.None);
            _selectedCardsInfo.Remove(existingSelection);
            Debug.Log($"Card {data.CharacterName} deselected.");
        }
        else if (_selectedCardsInfo.Count < CardsToSelect)
        {
            // 2. Картка не обрана і є вільні слоти. Виділяємо її.
            cardHandler.SetSelection(newMode);

            // Зберігаємо в _selectedCardsInfo
            _selectedCardsInfo.Add((data, cardHandler, newMode));
            Debug.Log($"Card {data.CharacterName} selected as {newMode}.");
        }
        else
        {
            // 3. Ліміт досягнуто. Дозволяємо лише скасування вибору.
            Debug.LogWarning($"P{_activePlayerID}: Досягнуто ліміту ({CardsToSelect}) карт. Скасуйте вибір, щоб вибрати іншу.");
        }

        // Оновлюємо фінальний список (використовується для перевірки кількості)
        _selectedCharacters = _selectedCardsInfo.Select(i => i.data).ToList();

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

        // 2. Зберігаємо невибрані карти (які більше не будуть доступні)
        _activePlayerHand.DiscardedCharacters.Clear();
        _activePlayerHand.DiscardedCharacters.AddRange(unselectedCharacters);

        Debug.Log($"P{_activePlayerID}: Draft confirmed. {CardsToSelect} selected, {unselectedCharacters.Count} discarded.");

        // !!! ЗБЕРІГАННЯ РЕЖИМІВ ВИБОРУ (Visible/Hidden) !!!
        // Передаємо список (Data, Mode) у PlayerHandData для використання в TraitPurchaseManager
        _activePlayerHand.SetSelectionModes(_selectedCardsInfo.Select(i => (i.data, i.mode)).ToList());
        // ------------------------------------------

        // 3. Очищаємо сцену та UI елементи
        CardManager.ClearCards();
        if (CardManager.CardsContainer != null)
        {
            CardManager.CardsContainer.gameObject.SetActive(false);
        }

        if (ConfirmSelectionButton != null) ConfirmSelectionButton.SetActive(false);

        // 4. Переходимо до наступного гравця/фази
        if (_activePlayerID == 1)
        {
            // Переходимо до P2
            _activePlayerID = 2;
            Debug.Log("Draft P1 Completed. Initiating Draft for Player 2.");

            // ТУТ ПОВИННА БУТИ ЛОГІКА HANDOVER UI
            // Наприклад: HandoverUI.ShowScreen(2, StartDraftPhase);
            StartDraftPhase(); // Тимчасовий прямий виклик для тестування
        }
        else
        {
            // Обидва гравці завершили драфт
            Debug.Log("Draft Phase Completed for both players.");

            // 5. Починаємо фазу купівлі рис (Trait Purchase)
            StartTraitPurchasePhase();
        }
    }


    // ----------------------------------------------------------------------
    // ФАЗА ПОКУПКИ РИС
    // ----------------------------------------------------------------------

    private void StartTraitPurchasePhase()
    {
        Debug.Log("Draft Phase Completed. Starting Trait Purchase Phase.");

        if (TraitPurchaseManager != null)
        {
            // Передаємо обидві руки менеджеру для початку фази
            TraitPurchaseManager.StartPurchasePhase(Player1Hand, Player2Hand);
        }
        else
        {
            Debug.LogError("TraitPurchaseManager не призначено. Неможливо перейти до наступної фази.");
            // Додайте логіку аварійного переходу, якщо TraitPurchaseManager відсутній
            // if (_gameManager != null) _gameManager.StartPlacementPhase();
        }
    }
}