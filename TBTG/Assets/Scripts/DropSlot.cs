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
    public float CardScaleFactor = 0.8f; // Масштаб картки в слоті

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

    private RectTransform _slotRectTransform;

    void Start()
    {
        _slotRectTransform = GetComponent<RectTransform>();
        UpdateVisuals();
    }

    public void OnDrop(PointerEventData eventData)
    {
        CardSelectionHandler draggedCard = eventData.pointerDrag?.GetComponent<CardSelectionHandler>();

        if (draggedCard != null)
        {
            if (CanAcceptCard(draggedCard))
            {
                AcceptCard(draggedCard);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging)
        {
            CardSelectionHandler draggedCard = eventData.pointerDrag?.GetComponent<CardSelectionHandler>();
            if (draggedCard != null && CanAcceptCard(draggedCard))
            {
                if (BackgroundImage != null)
                    BackgroundImage.color = HighlightColor;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisuals();
    }

    public bool CanAcceptCard(CardSelectionHandler card)
    {
        // Дозволяємо скидання навіть якщо слот зайнятий - картка буде замінена
        return true;
    }

    public void AcceptCard(CardSelectionHandler card)
    {
        // Видаляємо картку з попереднього слота
        RemoveCardFromPreviousSlot(card);

        // Якщо слот вже зайнятий - повертаємо стару картку на поле
        if (CurrentCard != null && CurrentCard != card)
        {
            CurrentCard.ReturnToDraftArea();
        }

        // Призначаємо нову картку
        CurrentCard = card;

        // Переміщуємо картку в слот
        card.MoveToSlot(transform, AutoScaleCard ? CardScaleFactor : 1f);

        // Оновлюємо режим вибору
        SelectionMode newMode = (SlotType == SlotType.Active) ? SelectionMode.Visible : SelectionMode.Hidden;
        card.SetSelection(newMode);

        Debug.Log($"Card {card.CardData.CharacterName} placed in {SlotType} slot");
    }

    private void RemoveCardFromPreviousSlot(CardSelectionHandler card)
    {
        DropSlot[] allSlots = FindObjectsOfType<DropSlot>();
        foreach (DropSlot slot in allSlots)
        {
            if (slot != this && slot.CurrentCard == card)
            {
                slot.ClearCardWithoutReturning();
            }
        }
    }

    public void RemoveCard()
    {
        if (CurrentCard != null)
        {
            CurrentCard.SetSelection(SelectionMode.None);
            CurrentCard.ReturnToDraftArea();
            CurrentCard = null;
        }
    }

    public void ClearCardWithoutReturning()
    {
        if (CurrentCard != null)
        {
            CurrentCard.SetSelection(SelectionMode.None);
            CurrentCard = null;
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