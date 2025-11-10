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

    [Tooltip("Рука гравця, куди ми зберігаємо обрані карти.")]
    public PlayerHandData PlayerHand;

    [Header("Draft UI")]
    [Tooltip("Кнопка, яка підтверджує вибір 8 карт.")]
    public GameObject ConfirmSelectionButton;

    [Tooltip("Необхідна кількість карт, яку гравець має обрати.")]
    public int CardsToSelect = 8;
    // -------------------------------------------------------------------


    [Header("Manager References")]
    public PlayerCardManager CardManager;

    private GameManager _gameManager;

    // Тимчасовий список усіх 10 карт, які зараз відображаються для вибору.
    private List<CardSelectionHandler> _activeDraftCards = new List<CardSelectionHandler>();

    // Список обраних карток
    private List<CharacterData> _selectedCharacters = new List<CharacterData>();

    private bool _isDraftPhaseActive = false;


    void Awake()
    {
        _gameManager = FindObjectOfType<GameManager>();
        Assert.IsNotNull(_gameManager, "GameManager не знайдено!");
    }

    void Start()
    {
        // Перевірка, що всі ScriptableObjects призначені
        Assert.IsNotNull(MasterDeck, "MasterDeck (тип MasterDeckData) не призначено!");
        Assert.IsNotNull(PlayerHand, "PlayerHand не призначено!");
        Assert.IsNotNull(CardManager, "CardManager не призначено!");

        // Налаштування кнопки підтвердження
        if (ConfirmSelectionButton != null)
        {
            ConfirmSelectionButton.SetActive(false);
            Button btn = ConfirmSelectionButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ConfirmSelection);
            }
        }
    }

    public void StartDraftPhase()
    {
        _isDraftPhaseActive = true;
        _selectedCharacters.Clear();
        PlayerHand.SelectedCharacters.Clear();

        // Отримуємо 10 унікальних карток для початкового драфту
        List<CharacterData> draftPool = GetUniqueRandomCards(10);

        DisplayDraftCards(draftPool);

        UpdateConfirmButtonState();

        Debug.Log("Draft Phase started. Select 8 characters.");
    }

    private List<CharacterData> GetUniqueRandomCards(int count)
    {
        // Використовуємо поле AllAvailableCharacters з MasterDeckData
        if (MasterDeck.AllAvailableCharacters == null || MasterDeck.AllAvailableCharacters.Count < count)
        {
            Debug.LogError($"Недостатньо карт у MasterDeck ({MasterDeck.AllAvailableCharacters?.Count ?? 0} доступно). Потрібно {count}.");
            return MasterDeck.AllAvailableCharacters?.Take(count).ToList() ?? new List<CharacterData>();
        }

        return MasterDeck.AllAvailableCharacters
                            .OrderBy(x => Random.value) // Рандомізація
                            .Take(count)                 // Вибір необхідної кількості
                            .ToList();
    }

    private void DisplayDraftCards(List<CharacterData> cards)
    {
        // Викликаємо public методи CardManager
        Vector3 scale = CardManager.CalculateInitialScale();

        CardManager.ClearCards(); // ВИПРАВЛЕННЯ CS0122: ClearCards тепер public

        _activeDraftCards.Clear();

        foreach (CharacterData data in cards)
        {
            // Створення та налаштування картки
            GameObject cardObject = Instantiate(CardManager.CharacterCardUIPrefab, CardManager.CardsContainer);
            cardObject.name = $"Draft_Card_{data.CharacterName}";

            CardScaler cardScaler = cardObject.GetComponent<CardScaler>();
            if (cardScaler != null)
            {
                cardScaler.SetInitialScale(scale);
            }

            CardSelectionHandler selectionHandler = cardObject.GetComponent<CardSelectionHandler>();
            if (selectionHandler == null)
            {
                selectionHandler = cardObject.AddComponent<CardSelectionHandler>();
            }

            selectionHandler.Initialize(data);
            selectionHandler.OnCardClicked += HandleCardClicked; // Підписка на клік

            _activeDraftCards.Add(selectionHandler);
        }
    }

    private void HandleCardClicked(CardSelectionHandler clickedCard)
    {
        if (!_isDraftPhaseActive) return;

        CharacterData data = clickedCard.CardData;

        if (clickedCard.IsSelected)
        {
            // Зняти вибір
            _selectedCharacters.Remove(data);
            clickedCard.SetSelection(false);
        }
        else
        {
            // Обрати
            if (_selectedCharacters.Count < CardsToSelect)
            {
                _selectedCharacters.Add(data);
                clickedCard.SetSelection(true);
            }
            else
            {
                Debug.Log("Ви вже обрали максимальну кількість карт.");
            }
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

        // 1. Зберігаємо обрані карти в PlayerHandData
        PlayerHand.SelectedCharacters.Clear();
        PlayerHand.SelectedCharacters.AddRange(_selectedCharacters);

        // 2. Очищаємо сцену
        CardManager.ClearCards();

        // 3. Приховуємо кнопку
        if (ConfirmSelectionButton != null) ConfirmSelectionButton.SetActive(false);

        // 4. Починаємо фазу формування пар (або переходимо далі)
        StartPairingPhase();
    }


    private void StartPairingPhase()
    {
        // ... Логіка підготовки до розміщення ...

        // ВИПРАВЛЕННЯ CS1061: CompleteDraftPhase має бути public у GameManager
        _gameManager.CompleteDraftPhase();
    }
}