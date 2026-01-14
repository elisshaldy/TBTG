using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class CardSlot : MonoBehaviour, IDropHandler
{
    public event Action CardAdded;
    public event Action CardRemoved;
    
    public RectTransform SlotPoint;

    public CardDragHandler CurrentCard;

    private void Awake()
    {
        if (SlotPoint == null)
            SlotPoint = transform as RectTransform;
    }

    public bool IsOccupied => CurrentCard != null;

    public void OnDrop(PointerEventData eventData)
    {
        CardDragHandler incomingCard = eventData.pointerDrag.GetComponent<CardDragHandler>();
        if (incomingCard == null) return;

        if (IsOccupied)
        {
            CardDragHandler existingCard = CurrentCard;
            CardSlot previousSlot = incomingCard.LastSlot;

            if (previousSlot != null)
            {
                // SWAP
                previousSlot.SetCardManually(existingCard);
            }
            else
            {
                existingCard.ReturnHome();
                existingCard.CurrentSlot = null;
            }
        }

        CurrentCard = incomingCard;
        incomingCard.CurrentSlot = this;

        incomingCard.SnapToSlot(SlotPoint);
        CardAdded?.Invoke();
    }

    public void SetCardManually(CardDragHandler card)
    {
        CurrentCard = card;
        card.CurrentSlot = this;
        card.SnapToSlot(SlotPoint);
        CardAdded?.Invoke();
    }

    public void ClearSlot()
    {
        CurrentCard = null;
        CardRemoved?.Invoke();
    }
}