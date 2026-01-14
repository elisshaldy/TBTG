using System;
using System.Collections.Generic;
using UnityEngine;

public class CardDeckController : MonoBehaviour
{
    public event Action<bool> DeckStateChanged; 

    [SerializeField] private List<CardSlot> _cardDeck;

    private void Awake()
    {
        SubscribeSlots();
        CheckDeck();
    }

    private void SubscribeSlots()
    {
        foreach (CardSlot slot in _cardDeck)
        {
            slot.CardAdded += CheckDeck;
            slot.CardRemoved += CheckDeck;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeSlots();
    }

    public void UnsubscribeSlots()
    {
        foreach (CardSlot slot in _cardDeck)
        {
            slot.CardAdded -= CheckDeck;
            slot.CardRemoved -= CheckDeck;
        }
    }

    private void CheckDeck()
    {
        bool isDeckFull = true;

        foreach (CardSlot slot in _cardDeck)
        {
            if (!slot.IsOccupied)
            {
                isDeckFull = false;
                break;
            }
        }

        DeckStateChanged?.Invoke(isDeckFull);
    }

    public void ToggleCardsRaycasts(bool value, CardDragHandler caller)
    {
        foreach (var slot in _cardDeck)
        {
            if (slot.CurrentCard != null && slot.CurrentCard != caller)
            {
                slot.CurrentCard.SetRaycastTarget(value);
            }
        }
    }
}