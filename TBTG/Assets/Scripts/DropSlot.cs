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

    // ЗМІНА: Робимо CurrentCard приватним і додаємо властивість
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

    void Start()
    {
        UpdateVisuals();
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"OnDrop called on {name}");

        CardSelectionHandler draggedCard = eventData.pointerDrag?.GetComponent<CardSelectionHandler>();

        if (draggedCard != null)
        {
            Debug.Log($"Card {draggedCard.CardData.CharacterName} dropped on slot {name}. CurrentCard: {CurrentCard?.CardData?.CharacterName ?? "NULL"}");

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
        // ПЕРЕВІРКА: чи слот вільний
        if (CurrentCard != null)
        {
            // Якщо це та сама картка, що вже в слоті - дозволяємо (для переміщення)
            if (CurrentCard == card)
            {
                Debug.Log($"Slot {name} already contains this card, allowing move");
                return true;
            }

            Debug.Log($"Slot {name} is occupied by {CurrentCard.CardData.CharacterName}");
            return false;
        }

        Debug.Log($"Slot {name} can accept card {card.CardData.CharacterName}");
        return true;
    }

    public void AcceptCard(CardSelectionHandler card)
    {
        Debug.Log($"AcceptCard called for {card.CardData.CharacterName} in slot {name}");

        // ВИДАЛЯЄМО картку з попереднього слота (якщо він є)
        RemoveCardFromPreviousSlot(card);

        // Очищаємо поточний слот (якщо потрібно)
        if (CurrentCard != null && CurrentCard != card)
        {
            CurrentCard.ReturnToOriginalPosition();
        }

        // Призначаємо нову картку
        CurrentCard = card;
        card.MoveToSlot(transform);

        // Оновлюємо режим вибору
        SelectionMode newMode = (SlotType == SlotType.Active) ? SelectionMode.Visible : SelectionMode.Hidden;
        card.SetSelection(newMode);

        Debug.Log($"Card {card.CardData.CharacterName} successfully placed in {SlotType} slot of pair {PairIndex}");
    }

    private void RemoveCardFromPreviousSlot(CardSelectionHandler card)
    {
        // Шукаємо всі слоти і видаляємо картку з них
        DropSlot[] allSlots = FindObjectsOfType<DropSlot>();
        foreach (DropSlot slot in allSlots)
        {
            if (slot != this && slot.CurrentCard == card)
            {
                Debug.Log($"Removing card from previous slot: {slot.name}");
                slot.ClearCardWithoutReturning();
            }
        }
    }

    public void RemoveCard()
    {
        if (CurrentCard != null)
        {
            Debug.Log($"Removing card {CurrentCard.CardData.CharacterName} from slot {name}");
            CurrentCard.SetSelection(SelectionMode.None);
            CurrentCard.ReturnToOriginalPosition();
            CurrentCard = null;
        }
    }

    public void ClearCardWithoutReturning()
    {
        if (CurrentCard != null)
        {
            Debug.Log($"Clearing card {CurrentCard.CardData.CharacterName} from slot {name} without returning");
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