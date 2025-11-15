using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Linq;

// Цей скрипт відповідає за динамічне створення та відображення всіх 
// карток персонажів гравця у відповідному контейнері UI.
public class PlayerCardManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Префаб CharacterCard_Panel (з прикріпленим CharacterCardUI та CardScaler), який ми будемо клонувати.")]
    public GameObject CharacterCardUIPrefab;

    [Tooltip("Контейнер UI, де будуть розміщені картки (Player_Cards_Area).")]
    public Transform CardsContainer;

    [Header("Data")]
    [Tooltip("Ассет, що містить список CharacterData, обраних гравцем.")]
    public PlayerHandData PlayerHand; // Посилання на PlayerHandData

    [Header("Scaling Settings")]
    [Tooltip("Кількість місць для карток. Використовується для розрахунку початкового масштабу.")]
    public int MaxCards = 8;

    [Tooltip("Коефіцієнт безпеки для гарантії, що картки помістяться. Наприклад, 0.95 зменшить розмір на 5%.")]
    [Range(0.8f, 1.0f)]
    public float ScaleSafetyMargin = 0.90f;

    [Tooltip("Базова ширина префаба. Візьміть значення Width з RectTransform префаба.")]
    public float BaseCardWidth = 300f;

    // Список усіх створених UI-карток на сцені.
    private List<CharacterCardUI> _spawnedCards = new List<CharacterCardUI>();

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
    /// ФІКСОВАНА ВЕРСІЯ - коректно встановлює масштаб
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

            if (cardUI != null)
            {
                _spawnedCards.Add(cardUI);
            }
        }

        Debug.Log($"Успішно завантажено {draftHandlers.Count} карток для драфту.");
        return draftHandlers;
    }

    /// <summary>
    /// Відображає сформовані пари в Player_Cards_Area
    /// ФІКСОВАНА ВЕРСІЯ - коректно керує масштабом
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
        rectTransform.sizeDelta = new Vector2(200, 300); // Налаштуйте розмір

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
            // Примусово скидаємо до початкового масштабу
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

    // ПУБЛІЧНИЙ МЕТОД: для виділення активної картки (якщо буде потрібно)
    public void HighlightCard(CharacterData data, bool highlight)
    {
        // Логіка підсвічування тут
        foreach (var cardUI in _spawnedCards)
        {
            if (cardUI != null && cardUI.GetCurrentData() == data)
            {
                // Тут можна додати логіку підсвічування
                // Наприклад, змінити колір рамки тощо
                break;
            }
        }
    }

    /// <summary>
    /// Оновлює масштаб всіх карток (корисно при зміні розміру екрану)
    /// </summary>
    public void UpdateAllCardsScale()
    {
        CalculateInitialScale(); // Перераховуємо масштаб

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
}