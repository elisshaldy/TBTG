using UnityEngine;
using UnityEngine.EventSystems;
using System; // Потрібно для System.Action
using UnityEngine.UI;

// Компонент, який дозволяє картці бути обраною/виділеною на етапі драфту.
// Реалізує IPointerClickHandler для обробки UI кліків.
public class CardSelectionHandler : MonoBehaviour, IPointerClickHandler
{
    // !!! ВИПРАВЛЕННЯ: ВНУТРІШНІЙ ENUM ВИДАЛЕНО !!!
    // Тепер використовується ГЛОБАЛЬНИЙ enum SelectionMode (з окремого файлу SelectionMode.cs).

    public SelectionMode CurrentMode { get; private set; } = SelectionMode.None;

    // Подія, яка тепер передає також дані кліку (для визначення ЛКМ/ПКМ) до GameDeckManager.
    public event Action<CardSelectionHandler, PointerEventData> OnCardClicked;

    [Tooltip("Посилання на CharacterData, яку відображає ця картка.")]
    public CharacterData CardData { get; private set; }

    [Header("Visuals")]
    [Tooltip("Об'єкт або Image, який відображає виділення (наприклад, рамка або галочка).")]
    public GameObject HighlightVisual;

    // Посилання на UI-компонент для відображення даних.
    private CharacterCardUI _cardUI;


    void Awake()
    {
        // Отримуємо компонент, який відповідає за відображення даних на картці UI
        _cardUI = GetComponent<CharacterCardUI>();

        // Переконуємося, що візуал вимкнений при старті
        // !!! ОНОВЛЕНО: Використовуємо глобальний SelectionMode !!!
        SetSelection(SelectionMode.None);
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
        // !!! ОНОВЛЕНО: Використовуємо глобальний SelectionMode !!!
        SetSelection(SelectionMode.None);
    }

    /// <summary>
    /// Змінює стан виділення та оновлює візуальне оформлення (Visible/Hidden/None).
    /// </summary>
    // !!! ОНОВЛЕНО: Тепер приймає глобальний SelectionMode, що вирішує CS1503 !!!
    public void SetSelection(SelectionMode newMode)
    {
        CurrentMode = newMode;
        // !!! ОНОВЛЕНО: Використовуємо глобальний SelectionMode !!!
        bool isSelected = newMode != SelectionMode.None;

        if (HighlightVisual != null)
        {
            HighlightVisual.SetActive(isSelected);

            // --- ДОДАТКОВА ЛОГІКА ДЛЯ ВІЗУАЛЬНОГО ВІДМІНЮВАННЯ ---
            if (HighlightVisual.TryGetComponent<Image>(out var image))
            {
                // !!! ОНОВЛЕНО: Використовуємо глобальний SelectionMode !!!
                if (newMode == SelectionMode.Visible)
                {
                    image.color = Color.yellow;
                }
                // !!! ОНОВЛЕНО: Використовуємо глобальний SelectionMode !!!
                else if (newMode == SelectionMode.Hidden)
                {
                    image.color = Color.gray;
                }
                // else залишається коректним
            }
            // --------------------------------------------------------
        }

        // Логіка для Debug.Log, якщо необхідно відслідковувати стан
        // !!! ОНОВЛЕНО: Використовуємо глобальний SelectionMode !!!
        if (newMode == SelectionMode.Visible)
        {
            Debug.Log($"Card {CardData.CharacterName} selected as VISIBLE (LKM).");
        }
        // !!! ОНОВЛЕНО: Використовуємо глобальний SelectionMode !!!
        else if (newMode == SelectionMode.Hidden)
        {
            Debug.Log($"Card {CardData.CharacterName} selected as HIDDEN (PKM).");
        }
    }

    // Обробка кліку: вимагається інтерфейсом IPointerClickHandler.
    public void OnPointerClick(PointerEventData eventData)
    {
        // Повідомляємо менеджеру, що картку було клікнуто, передаючи дані про тип кліку.
        OnCardClicked?.Invoke(this, eventData);
    }
}