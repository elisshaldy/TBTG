using UnityEngine;
using UnityEngine.EventSystems;
using System; // Потрібно для System.Action

// Компонент, який дозволяє картці бути обраною/виділеною на етапі драфту.
// Реалізує IPointerClickHandler для обробки UI кліків.
public class CardSelectionHandler : MonoBehaviour, IPointerClickHandler
{
    // Подія, яку викликаємо при кліку (повідомляємо GameDeckManager).
    public event Action<CardSelectionHandler> OnCardClicked;

    [Tooltip("Посилання на CharacterData, яку відображає ця картка.")]
    public CharacterData CardData { get; private set; }

    [Header("Visuals")]
    [Tooltip("Об'єкт або Image, який відображає виділення (наприклад, рамка або галочка).")]
    public GameObject HighlightVisual;

    public bool IsSelected { get; private set; } = false;

    // Посилання на UI-компонент для відображення даних.
    // ПРИМІТКА: Для роботи цього поля, клас CharacterCardUI повинен існувати.
    private CharacterCardUI _cardUI;


    void Awake()
    {
        // Отримуємо компонент, який відповідає за відображення даних на картці UI
        _cardUI = GetComponent<CharacterCardUI>();

        // Переконуємося, що візуал виділення вимкнений при старті
        if (HighlightVisual != null)
        {
            HighlightVisual.SetActive(false);
        }
    }

    /// <summary>
    /// Викликається GameDeckManager для ініціалізації картки даними.
    /// </summary>
    /// <param name="data">Дані персонажа.</param>
    public void Initialize(CharacterData data)
    {
        CardData = data;

        // Відображаємо дані персонажа на UI-елементах картки.
        if (_cardUI != null)
        {
            _cardUI.DisplayCharacter(data);
        }

        // Скидаємо стан виділення
        SetSelection(false);
    }

    /// <summary>
    /// Змінює стан виділення та оновлює візуальне оформлення.
    /// </summary>
    /// <param name="isSelected">True, якщо картка обрана.</param>
    public void SetSelection(bool isSelected)
    {
        IsSelected = isSelected;

        if (HighlightVisual != null)
        {
            HighlightVisual.SetActive(isSelected);
        }

        if (isSelected)
        {
            Debug.Log($"Card {CardData.CharacterName} selected.");
        }
    }

    // Обробка кліку: вимагається інтерфейсом IPointerClickHandler.
    public void OnPointerClick(PointerEventData eventData)
    {
        // Повідомляємо менеджеру, що картку було клікнуто.
        OnCardClicked?.Invoke(this);
    }
}