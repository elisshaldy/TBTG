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
        CardDragHandler card = eventData.pointerDrag.GetComponent<CardDragHandler>();
        if (card == null) return;

        if (IsOccupied)
        {
            card.ReturnHome();
            return;
        }

        if (card.CurrentSlot != null)
        {
            card.CurrentSlot.ClearSlot();
        }

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