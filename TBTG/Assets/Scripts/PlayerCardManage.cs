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


    void Awake()
    {
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
    /// !!! ВИПРАВЛЕННЯ CS1061: ДОДАНО МЕТОД LoadDraftCards !!!
    /// Створює UI-картки на сцені для фази драфту.
    /// Повертає список CardSelectionHandler для підписки на події.
    /// </summary>
    public List<CardSelectionHandler> LoadDraftCards(List<CharacterData> draftCharacters)
    {
        // Очищаємо всі старі картки
        ClearCards();

        List<CardSelectionHandler> draftHandlers = new List<CardSelectionHandler>();

        foreach (CharacterData data in draftCharacters)
        {
            // Створення об'єкта
            GameObject cardObject = Instantiate(CharacterCardUIPrefab, CardsContainer);
            cardObject.name = $"Card_Draft_{data.CharacterName}";

            // Отримання компонентів
            CharacterCardUI cardUI = cardObject.GetComponent<CharacterCardUI>();
            CardSelectionHandler selectionHandler = cardObject.GetComponent<CardSelectionHandler>();
            CardScaler cardScaler = cardObject.GetComponent<CardScaler>();

            if (selectionHandler == null)
            {
                Debug.LogError($"Префаб {CharacterCardUIPrefab.name} не має компонента CardSelectionHandler! Draft не працюватиме.");
                Destroy(cardObject);
                continue;
            }

            // Ініціалізація Selection Handler (він внутрішньо викликає DisplayCharacter)
            selectionHandler.Initialize(data);
            draftHandlers.Add(selectionHandler);

            // Зберігаємо UI-компонент у список для майбутнього очищення
            if (cardUI != null)
            {
                _spawnedCards.Add(cardUI);
            }

            // Застосовуємо розрахований масштаб
            if (cardScaler != null)
            {
                // Для CardScaler, який не має публічного сеттера InitialScale,
                // але має публічну властивість.
                // Оскільки в CardScaler.cs вона: public Vector3 InitialScale { get; private set; } = Vector3.one;
                // Ми можемо використовувати простий transform.localScale, якщо властивість приватна.
                // Або змінити CardScaler, щоб він мав публічний метод.
                // Припускаємо, що простий transform.localScale працює для початку.
                cardObject.transform.localScale = _calculatedInitialScale;
            }
        }

        Debug.Log($"Успішно завантажено {draftHandlers.Count} карток для драфту.");
        return draftHandlers;
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
    }
}