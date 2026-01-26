using System;
using System.Collections.Generic;
using UnityEngine;

public class CardDeckController : MonoBehaviour
{
    public event Action<bool> DeckStateChanged; 

    [SerializeField] private PlayerModel _playerModel;
    
    [SerializeField] private List<CardSlot> _cardDeck;
    [SerializeField] private List<CardDragHandler> _cards;
    [SerializeField] private List<ModDragHandler> _mods;

    private void Awake()
    {
        SubscribeSlots();
        CheckDeck();
    }

    public void RegisterCard(CardDragHandler card)
    {
        if (card == null) return;
        if (!_cards.Contains(card)) _cards.Add(card);
    }

    public void RegisterMod(ModDragHandler mod)
    {
        if (mod == null) return;
        
        // Відписуємося про всяк випадок, щоб не було подвійних підписок
        mod.ModAttachHere -= _playerModel.SpendPoints;
        mod.ModDetachHere -= _playerModel.RefundPoints;
        
        mod.ModAttachHere += _playerModel.SpendPoints;
        mod.ModDetachHere += _playerModel.RefundPoints;
        
        if (!_mods.Contains(mod)) _mods.Add(mod);
    }

    private void SubscribeSlots()
    {
        foreach (CardSlot slot in _cardDeck)
        {
            if (slot == null) continue;
            slot.CardAdded += CheckDeck;
            slot.CardRemoved += CheckDeck;
        }

        foreach (ModDragHandler mod in _mods)
        {
            RegisterMod(mod);
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
        
        // foreach (ModDragHandler mod in _mods)
        // {
        //     mod.ModAttachHere -= _playerModel.SpendPoints;
        // }
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