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

    public void ResetController()
    {
        foreach (var card in _cards)
        {
            if (card != null) card.gameObject.SetActive(false);
        }
        
        foreach (var slot in _cardDeck)
        {
            if (slot != null && slot.IsOccupied)
            {
                slot.ClearSlot();
            }
        }

        _cards.Clear();
        _mods.Clear();
        CheckDeck();
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

    public List<GameObject> GetSelectedCards()
    {
        List<GameObject> selected = new List<GameObject>();
        foreach (var slot in _cardDeck)
        {
            if (slot.IsOccupied && slot.CurrentCard != null)
            {
                selected.Add(slot.CurrentCard.gameObject);
            }
        }
        return selected;
    }

    public void AutoFillSlots()
    {
        int cardIdx = 0;
        foreach (var slot in _cardDeck)
        {
            if (slot == null || slot.IsOccupied) continue;

            // Шукаємо першу вільну карту, яка ще не в слоті
            while (cardIdx < _cards.Count && (_cards[cardIdx] == null || _cards[cardIdx].CurrentSlot != null || !_cards[cardIdx].gameObject.activeInHierarchy))
            {
                cardIdx++;
            }

            if (cardIdx < _cards.Count)
            {
                slot.SetCardManually(_cards[cardIdx]);
                cardIdx++;
            }
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