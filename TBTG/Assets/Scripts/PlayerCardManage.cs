using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Linq;

public class PlayerCardManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Префаб CharacterCard_Panel (з прикріпленим CharacterCardUI та CardScaler), який ми будемо клонувати.")]
    public GameObject CharacterCardUIPrefab;

    [Tooltip("Контейнер UI, де будуть розміщені картки (Player_Cards_Area).")]
    public Transform CardsContainer;

    [Header("Data")]
    [Tooltip("Ассет, що містить список CharacterData, обраних гравцем.")]
    public PlayerHandData PlayerHand;

    [Header("Scaling Settings")]
    [Tooltip("Кількість місць для карток. Використовується для розрахунку початкового масштабу.")]
    public int MaxCards = 8;

    [Tooltip("Коефіцієнт безпеки для гарантії, що картки помістяться.")]
    [Range(0.8f, 1.0f)]
    public float ScaleSafetyMargin = 0.90f;

    [Tooltip("Базова ширина префаба. Візьміть значення Width з RectTransform префаба.")]
    public float BaseCardWidth = 300f;

    // Список усіх створених UI-карток на сцені.
    private List<CharacterCardUI> _spawnedCards = new List<CharacterCardUI>();
    private List<CardSelectionHandler> _cardHandlers = new List<CardSelectionHandler>();

    // Розрахований початковий масштаб
    private Vector3 _calculatedInitialScale = Vector3.one;

    // Singleton для доступу з інших скриптів
    public static PlayerCardManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Assert.IsNotNull(CharacterCardUIPrefab, "CharacterCardUIPrefab не призначено в PlayerCardManager.");
        Assert.IsNotNull(CardsContainer, "CardsContainer не призначено в PlayerCardManager.");

        // Розрахунок масштабу робимо один раз при старті
        CalculateInitialScale();
    }

    private void CalculateInitialScale()
    {
        RectTransform containerRect = CardsContainer.GetComponent<RectTransform>();
        if (containerRect == null)
        {
            Debug.LogError("CardsContainer не має компонента RectTransform. Неможливо розрахувати масштаб.");
            return;
        }

        // 1. Ширина контейнера
        float containerWidth = containerRect.rect.width;

        // 2. Ширина, потрібна для розміщення MaxCards (враховуючи невеликий запас)
        float totalRequiredWidth = BaseCardWidth * MaxCards;

        // 3. Коефіцієнт масштабування
        float scaleFactor = 1f;
        if (totalRequiredWidth > containerWidth)
        {
            scaleFactor = (containerWidth / totalRequiredWidth) * ScaleSafetyMargin;
        }

        _calculatedInitialScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        Debug.Log($"Initial Card Scale calculated: {_calculatedInitialScale.x}");
    }

    /// <summary>
    /// Створює UI-картки на сцені для фази драфту.
    /// </summary>
    public List<CardSelectionHandler> LoadDraftCards(List<CharacterData> draftCharacters)
    {
        ClearCards();

        List<CardSelectionHandler> draftHandlers = new List<CardSelectionHandler>();

        foreach (CharacterData data in draftCharacters)
        {
            GameObject cardObject = Instantiate(CharacterCardUIPrefab, CardsContainer);
            cardObject.name = $"Card_Draft_{data.CharacterName}";

            CharacterCardUI cardUI = cardObject.GetComponent<CharacterCardUI>();
            CardSelectionHandler selectionHandler = cardObject.GetComponent<CardSelectionHandler>();
            CardScaler cardScaler = cardObject.GetComponent<CardScaler>();

            if (selectionHandler == null)
            {
                Debug.LogError($"Префаб {CharacterCardUIPrefab.name} не має компонента CardSelectionHandler!");
                Destroy(cardObject);
                continue;
            }

            // ВАЖЛИВО: Спочатку встановлюємо початковий масштаб
            if (cardScaler != null)
            {
                cardScaler.SetInitialScale(_calculatedInitialScale);
            }

            // Потім ініціалізуємо Selection Handler
            selectionHandler.Initialize(data);
            draftHandlers.Add(selectionHandler);
            _cardHandlers.Add(selectionHandler);

            // Підписуємось на події картки
            selectionHandler.OnCardDropped += OnCardDropped;
            selectionHandler.OnCardReturnedToDraft += OnCardReturnedToDraft;

            if (cardUI != null)
            {
                _spawnedCards.Add(cardUI);
            }
        }

        Debug.Log($"Успішно завантажено {draftHandlers.Count} карток для драфту.");
        return draftHandlers;
    }

    /// <summary>
    /// Обробка події, коли картка розміщена в слоті
    /// </summary>
    private void OnCardDropped(CardSelectionHandler card, DropSlot slot)
    {
        Debug.Log($"Card {card.CardData.CharacterName} dropped into slot {slot.name}");

        // Тут можна додати додаткову логіку при розміщенні картки в слот
        // Наприклад, оновлення статистики гравця тощо
    }

    /// <summary>
    /// Обробка події, коли картка повернута на поле вибору
    /// </summary>
    private void OnCardReturnedToDraft(CardSelectionHandler card)
    {
        Debug.Log($"Card {card.CardData.CharacterName} returned to draft area");

        // Тут можна додати логіку при поверненні картки на поле
    }

    /// <summary>
    /// Відображає сформовані пари в Player_Cards_Area
    /// </summary>
    public void DisplayFormedPairs(List<CharacterPair> pairs)
    {
        ClearCards();

        foreach (var pair in pairs)
        {
            CreatePairCard(pair);
        }

        Debug.Log($"Відображено {pairs.Count} пар у Player_Cards_Area");
    }

    private void CreatePairCard(CharacterPair pair)
    {
        // Створюємо контейнер для пари
        GameObject pairContainer = new GameObject($"Pair_{pair.ActiveCharacter.CharacterName}");
        pairContainer.transform.SetParent(CardsContainer, false);

        // Додаємо RectTransform для правильного позиціонування
        var rectTransform = pairContainer.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 300);

        // Створюємо картки пари
        CreateCharacterCard(pair.ActiveCharacter, pairContainer.transform, "Active");
        CreateCharacterCard(pair.HiddenCharacter, pairContainer.transform, "Hidden");
    }

    private void CreateCharacterCard(CharacterData data, Transform parent, string type)
    {
        GameObject cardObject = Instantiate(CharacterCardUIPrefab, parent);
        cardObject.name = $"{type}_{data.CharacterName}";

        CharacterCardUI cardUI = cardObject.GetComponent<CharacterCardUI>();
        CardScaler cardScaler = cardObject.GetComponent<CardScaler>();
        CardSelectionHandler selectionHandler = cardObject.GetComponent<CardSelectionHandler>();

        if (cardUI != null)
        {
            cardUI.DisplayCharacter(data);
        }

        // ВАЖЛИВО: Встановлюємо правильний масштаб
        if (cardScaler != null)
        {
            cardScaler.SetInitialScale(_calculatedInitialScale);
            cardScaler.ResetToInitialScale();
        }

        // Вимкнути можливість вибору для вже сформованих пар
        if (selectionHandler != null)
        {
            selectionHandler.enabled = false;
        }

        _spawnedCards.Add(cardUI);
    }

    /// <summary>
    /// Очищає контейнер карток.
    /// </summary>
    public void ClearCards()
    {
        // Відписуємось від подій
        foreach (var handler in _cardHandlers)
        {
            if (handler != null)
            {
                handler.OnCardDropped -= OnCardDropped;
                handler.OnCardReturnedToDraft -= OnCardReturnedToDraft;
            }
        }
        _cardHandlers.Clear();

        // Видаляємо картки
        foreach (CharacterCardUI cardUI in _spawnedCards)
        {
            if (cardUI != null && cardUI.gameObject != null)
            {
                Destroy(cardUI.gameObject);
            }
        }
        _spawnedCards.Clear();

        Debug.Log("PlayerCardManager: All cards cleared from scene.");
    }

    /// <summary>
    /// Оновлює масштаб всіх карток
    /// </summary>
    public void UpdateAllCardsScale()
    {
        CalculateInitialScale();

        foreach (var cardUI in _spawnedCards)
        {
            if (cardUI != null)
            {
                CardScaler cardScaler = cardUI.GetComponent<CardScaler>();
                if (cardScaler != null)
                {
                    cardScaler.SetInitialScale(_calculatedInitialScale);
                    cardScaler.ResetToInitialScale();
                }
            }
        }
    }

    /// <summary>
    /// Повертає картку на поле вибору (корисно для тестування)
    /// </summary>
    public void ReturnCardToDraft(CharacterData data)
    {
        foreach (var handler in _cardHandlers)
        {
            if (handler != null && handler.CardData == data && handler.IsInSlot())
            {
                handler.ReturnToDraftArea();
                break;
            }
        }
    }

    /// <summary>
    /// Логує статус всіх карток (для дебагу)
    /// </summary>
    public void LogAllCardsStatus()
    {
        Debug.Log($"=== CARDS STATUS ({_cardHandlers.Count} cards) ===");

        foreach (var handler in _cardHandlers)
        {
            if (handler != null)
            {
                string status = handler.IsInSlot() ?
                    $"IN SLOT {handler.GetCurrentSlot()?.name}" :
                    "IN DRAFT AREA";
                Debug.Log($"Card '{handler.CardData.CharacterName}': {status}");
            }
        }
    }
}