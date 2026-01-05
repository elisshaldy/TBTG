using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Settings")]
    public SlotType SlotType = SlotType.Active;
    public int PairIndex = 0;

    [Header("Visual Feedback")]
    public Image BackgroundImage;
    public Color NormalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color HighlightColor = new Color(0.5f, 0.8f, 0.5f, 0.7f);
    public Color OccupiedColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);

    [Header("Scaling Settings")]
    public bool AutoScaleCard = true;
    public float CardScaleFactor = 0.8f;

    [Header("Additional Visual Feedback")]
    public GameObject DropIndicator;

    private CardSelectionHandler _currentCard;
    public CardSelectionHandler CurrentCard
    {
        get => _currentCard;
        private set
        {
            _currentCard = value;
            UpdateVisuals();
        }
    }

    private VerticalLayoutGroup _layoutGroup;
    private bool _hasLayoutGroup;

    void Start()
    {
        _layoutGroup = GetComponent<VerticalLayoutGroup>();
        _hasLayoutGroup = (_layoutGroup != null);

        UpdateVisuals();
    }

    public void OnDrop(PointerEventData eventData)
    {
        CardSelectionHandler draggedCard = eventData.pointerDrag?.GetComponent<CardSelectionHandler>();

        if (draggedCard != null)
        {
            AcceptCard(draggedCard);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging)
        {
            CardSelectionHandler draggedCard = eventData.pointerDrag?.GetComponent<CardSelectionHandler>();
            if (draggedCard != null && CanAcceptCard(draggedCard))
            {
                ShowCanDropFeedback();
            }
            else
            {
                ShowCannotDropFeedback();
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisuals();
    }

    public void AcceptCard(CardSelectionHandler newCard)
    {
        Debug.Log($"AcceptCard called for {newCard.CardData.CharacterName} in slot {name}");

        // Якщо це та сама картка, що вже в слоті - нічого не робимо
        if (CurrentCard == newCard)
        {
            Debug.Log("Same card, no replacement needed");
            return;
        }

        // Перевірка, чи можемо прийняти картку
        if (!CanAcceptCard(newCard))
        {
            Debug.Log($"Cannot accept card {newCard.CardData.CharacterName} in slot {name}");
            return;
        }

        // Зберігаємо посилання на стару картку
        CardSelectionHandler oldCard = CurrentCard;

        // Видаляємо нову картку з попереднього слота
        RemoveCardFromPreviousSlot(newCard);

        // ТИМЧАСОВО ВИМИКАЄМО LAYOUT GROUP для уникнення артефактів
        bool wasLayoutEnabled = false;
        if (_hasLayoutGroup && _layoutGroup.enabled)
        {
            wasLayoutEnabled = true;
            _layoutGroup.enabled = false;
        }

        try
        {
            // Призначаємо нову картку в поточний слот
            CurrentCard = newCard;
            newCard.MoveToSlot(transform, this, AutoScaleCard ? CardScaleFactor : 1f);
            newCard.OnPlacedInSlot(this);

            // Якщо була стара картка - повертаємо її на поле вибору
            if (oldCard != null)
            {
                Debug.Log($"Replacing old card {oldCard.CardData.CharacterName} with new card {newCard.CardData.CharacterName}");
                oldCard.ReturnToDraftArea();
            }

            Debug.Log($"Card {newCard.CardData.CharacterName} successfully placed in slot {name}");
        }
        finally
        {
            // ЗАВЖДИ увімкнути Layout Group знову
            if (_hasLayoutGroup && wasLayoutEnabled)
            {
                _layoutGroup.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Перевіряє, чи може слот прийняти картку
    /// </summary>
    private bool CanAcceptCard(CardSelectionHandler card)
    {
        if (card == null) return false;
        if (card == CurrentCard) return false;

        // Тут можна додати додаткові умови (наприклад, перевірка типу картки)
        return true;
    }

    /// <summary>
    /// Видаляє картку з попереднього слота
    /// </summary>
    private void RemoveCardFromPreviousSlot(CardSelectionHandler card)
    {
        if (card == null) return;

        // Шукаємо всі слоти на сцені
        DropSlot[] allSlots = FindObjectsOfType<DropSlot>();
        foreach (DropSlot slot in allSlots)
        {
            if (slot != this && slot.CurrentCard == card)
            {
                Debug.Log($"Removing card {card.CardData.CharacterName} from previous slot: {slot.name}");
                slot.ClearCardWithoutReturning();
                break; // Картка може бути тільки в одному слоті
            }
        }
    }

    /// <summary>
    /// Видаляє картку зі слота і повертає її на поле вибору
    /// </summary>
    public void RemoveCard()
    {
        if (CurrentCard != null)
        {
            Debug.Log($"Removing card {CurrentCard.CardData.CharacterName} from slot {name}");
            CurrentCard.OnRemovedFromSlot();
            CurrentCard.ReturnToDraftArea();
            CurrentCard = null;

            // УВІМКНУТИ LAYOUT GROUP знову
            if (_hasLayoutGroup)
            {
                _layoutGroup.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }
        }
    }

    /// <summary>
    /// Очищає слот без повернення картки на поле вибору
    /// </summary>
    public void ClearCardWithoutReturning()
    {
        if (CurrentCard != null)
        {
            Debug.Log($"Clearing card {CurrentCard.CardData.CharacterName} from slot {name} without returning");
            CurrentCard.OnRemovedFromSlot();
            CurrentCard = null;

            // УВІМКНУТИ LAYOUT GROUP знову
            if (_hasLayoutGroup)
            {
                _layoutGroup.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }
        }
    }

    private void UpdateVisuals()
    {
        if (BackgroundImage != null)
        {
            if (CurrentCard != null)
            {
                BackgroundImage.color = OccupiedColor;
            }
            else
            {
                BackgroundImage.color = NormalColor;
            }
        }

        // Індикатор можливості дропу
        if (DropIndicator != null)
        {
            DropIndicator.SetActive(CurrentCard == null);
        }
    }

    public void ShowCanDropFeedback()
    {
        if (BackgroundImage != null)
            BackgroundImage.color = HighlightColor;
    }

    public void ShowCannotDropFeedback()
    {
        if (BackgroundImage != null)
            BackgroundImage.color = OccupiedColor;
    }

    public bool IsOccupied()
    {
        return CurrentCard != null;
    }

    public CharacterData GetCardData()
    {
        return CurrentCard?.CardData;
    }

    public bool ContainsCard(CardSelectionHandler card)
    {
        return CurrentCard == card;
    }
}

public enum SlotType
{
    Active,
    Hidden
}