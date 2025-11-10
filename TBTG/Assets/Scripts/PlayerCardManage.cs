using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UI;

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
    public PlayerHandData PlayerHand;

    [Header("Scaling Settings")]
    [Tooltip("Кількість місць для карток. Використовується для розрахунку початкового масштабу.")]
    public int MaxCards = 8;

    [Tooltip("Коефіцієнт безпеки для гарантії, що картки помістяться. Наприклад, 0.95 зменшить розмір на 5%.")]
    [Range(0.8f, 1.0f)]
    public float ScaleSafetyMargin = 0.90f; // За замовчуванням 0.90

    [Tooltip("Базова ширина префаба. Візьміть значення Width з RectTransform префаба!")]
    public float PrefabBaseWidth = 100f; // !!! ПОВИННО БУТИ НАЛАШТОВАНЕ ВРУЧНУ !!!

    [Tooltip("Базова висота префаба. Візьміть значення Height з RectTransform префаба!")]
    public float PrefabBaseHeight = 150f; // !!! ПОВИННО БУТИ НАЛАШТОВАНЕ ВРУЧНУ !!!


    // Початковий масштаб, розрахований на основі розміру контейнера
    private Vector3 _calculatedInitialScale = Vector3.one;

    // Список створених екземплярів UI, щоб ми могли звертатися до них пізніше.
    private List<CharacterCardUI> _spawnedCards = new List<CharacterCardUI>();

    void Start()
    {
        // Перевірка критичних посилань
        Assert.IsNotNull(CharacterCardUIPrefab, "CharacterCardUIPrefab не призначено! Призначте префаб картки.");
        Assert.IsNotNull(CardsContainer, "CardsContainer (Player_Cards_Area) не призначено!");
        Assert.IsNotNull(PlayerHand, "PlayerHandData не призначено! Призначте ассет з даними персонажів.");

        // Перевірка, чи встановлено базові розміри префаба.
        if (PrefabBaseWidth <= 0 || PrefabBaseHeight <= 0)
        {
            RectTransform cardRect = CharacterCardUIPrefab.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                // Використовуємо .sizeDelta для отримання Width/Height, оскільки .rect може бути невірним у префабі.
                PrefabBaseWidth = cardRect.sizeDelta.x;
                PrefabBaseHeight = cardRect.sizeDelta.y;
                Debug.LogWarning($"PrefabBaseWidth/Height не було встановлено. Встановлено з RectTransform: W={PrefabBaseWidth}, H={PrefabBaseHeight}.");
            }
        }

        if (PlayerHand != null && CardsContainer != null && CharacterCardUIPrefab != null && PrefabBaseWidth > 0 && PrefabBaseHeight > 0)
        {
            // Якщо все призначено, запускаємо процес створення карток
            _calculatedInitialScale = CalculateInitialScale();
            LoadPlayerCards();
        }
    }

    /// <summary>
    /// Обчислює необхідний початковий масштаб, щоб MaxCards помістилися в контейнер, 
    /// враховуючи обмеження як по ВИСОТІ (пріоритет), так і по ШИРИНІ.
    /// </summary>
    public Vector3 CalculateInitialScale()
    {
        RectTransform containerRect = CardsContainer.GetComponent<RectTransform>();
        HorizontalOrVerticalLayoutGroup layoutGroup = CardsContainer.GetComponent<HorizontalOrVerticalLayoutGroup>();

        if (containerRect == null || layoutGroup == null)
        {
            Debug.LogError("Контейнер не має RectTransform/LayoutGroup! Неможливо розрахувати масштаб.");
            return Vector3.one;
        }

        // 1. ПРІОРИТЕТ: Розрахунок коефіцієнта масштабування на основі ВИСОТИ
        // Мета: Зменшити картку, щоб її висота точно відповідала висоті контейнера.

        float containerHeight = containerRect.rect.height;

        // Віднімаємо верхній і нижній відступи
        float availableCardHeight = containerHeight - (layoutGroup.padding.top + layoutGroup.padding.bottom);

        // Обчислюємо коефіцієнт масштабування, необхідний для того, щоб поміститися по висоті
        float scaleFactorByHeight = availableCardHeight / PrefabBaseHeight;

        // Встановлюємо цей масштаб як робочий
        float workingScaleFactor = scaleFactorByHeight;


        // 2. ВЕРИФІКАЦІЯ: Перевірка, чи вміщаються картки по ШИРИНІ з цим робочим масштабом

        float containerWidth = containerRect.rect.width;

        // Базова ширина картки після масштабування за висотою
        float scaledCardWidth = PrefabBaseWidth * workingScaleFactor;

        float totalPaddingWidth = layoutGroup.padding.left + layoutGroup.padding.right;
        float totalSpacingWidth = layoutGroup.spacing * (MaxCards - 1);

        // Загальна необхідна ширина, якщо ми використовуємо масштаб по висоті
        float totalRequiredWidth = (scaledCardWidth * MaxCards) + totalPaddingWidth + totalSpacingWidth;

        // Якщо необхідна ширина перевищує доступну ширину контейнера, ми повинні зменшити масштаб!
        if (totalRequiredWidth > containerWidth)
        {
            Debug.LogWarning("Карток забагато! Зменшуємо масштаб, щоб поміститися по ширині.");

            // Розраховуємо, наскільки потрібно зменшити масштаб, щоб поміститися по ширині
            float totalAvailableWidthForCards = containerWidth - totalPaddingWidth - totalSpacingWidth;
            float maxScaledCardWidth = totalAvailableWidthForCards / MaxCards;

            // Новий коефіцієнт масштабування, обмежений шириною
            float scaleFactorByWidth = maxScaledCardWidth / PrefabBaseWidth;

            // Використовуємо менший коефіцієнт
            workingScaleFactor = scaleFactorByWidth;
        }


        // 3. Фіналізація: Застосування Маржі Безпеки та Обмеження

        workingScaleFactor *= ScaleSafetyMargin;

        // Обмежуємо масштаб
        workingScaleFactor = Mathf.Clamp(workingScaleFactor, 0.05f, 1.0f); // 0.05f як мінімум


        Debug.Log($"W Container: {containerWidth}, H Container: {containerHeight}. Prefab H: {PrefabBaseHeight}. Initial Scale by H: {scaleFactorByHeight:F2}. Final Scale: {workingScaleFactor:F2}");
        return new Vector3(workingScaleFactor, workingScaleFactor, workingScaleFactor);
    }


    /// <summary>
    /// Динамічно створює UI-картки на основі даних PlayerHandData та розміщує їх у контейнері.
    /// </summary>
    public void LoadPlayerCards()
    {
        ClearCards();

        if (PlayerHand.SelectedCharacters.Count == 0)
        {
            Debug.LogWarning("У PlayerHandData немає обраних персонажів для відображення.");
            return;
        }

        // Якщо MaxCards > PlayerHand.SelectedCharacters.Count, ми використовуємо фактичну кількість.
        // Це важливе виправлення, оскільки MaxCards використовується у формулі розрахунку відстаней.
        // Наразі залишимо, як є, припускаючи, що MaxCards >= PlayerHand.SelectedCharacters.Count
        int actualCardsToDisplay = PlayerHand.SelectedCharacters.Count;

        for (int i = 0; i < actualCardsToDisplay; i++)
        {
            CharacterData data = PlayerHand.SelectedCharacters[i];

            if (data == null)
            {
                Debug.LogWarning("Знайдено NULL-посилання в списку CharacterData. Пропущено.");
                continue;
            }

            GameObject cardObject = Instantiate(CharacterCardUIPrefab, CardsContainer);
            cardObject.name = $"Card_UI_{data.CharacterName}";

            CharacterCardUI cardUI = cardObject.GetComponent<CharacterCardUI>();
            CardScaler cardScaler = cardObject.GetComponent<CardScaler>();

            if (cardUI != null)
            {
                cardUI.DisplayCharacter(data);
                _spawnedCards.Add(cardUI);
            }

            if (cardScaler != null)
            {
                // Встановлюємо початковий масштаб, обчислений функцією CalculateInitialScale
                cardScaler.SetInitialScale(_calculatedInitialScale);
            }
            else
            {
                Debug.LogError($"Префаб {CharacterCardUIPrefab.name} не має компонента CardScaler! Масштабування не працюватиме.");
            }
        }

        Debug.Log($"Успішно завантажено {_spawnedCards.Count} карток персонажів.");
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
    }

    // ПУБЛІЧНИЙ МЕТОД: для виділення активної картки (якщо буде потрібно)
    public void HighlightCard(CharacterData dataToHighlight, bool isHighlighted)
    {
        // Тут буде логіка пошуку відповідної картки у _spawnedCards та її візуальне виділення.
    }
}