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
        if (!_cards.Contains(card))
        {
            _cards.Add(card);
            
            // Pairing logic: if we just added an odd index card, pair it with the previous even index card
            int count = _cards.Count;
            if (count % 2 == 0)
            {
                CardDragHandler card1 = _cards[count - 2];
                CardDragHandler card2 = _cards[count - 1];
                card1.PartnerCard = card2;
                card2.PartnerCard = card1;

                card1.IsPassive = false; // Left card - Active
                card2.IsPassive = true;  // Right card - Passive/Hidden
            }
        }
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

        for (int i = 0; i < _cardDeck.Count; i++)
        {
            CardSlot slot = _cardDeck[i];
            if (!slot.IsOccupied)
            {
                isDeckFull = false;
                continue;
            }

            CardDragHandler card = slot.CurrentCard;
            bool wasPassive = card.IsPassive;
            bool nowPassive = (i % 2 != 0); // Slot 0, 2, 4... are Active. Slot 1, 3, 5... are Passive.

            card.IsPassive = nowPassive;

            // If a card was Passive and became Active (or vice versa) and it's already on the field, we need to swap models
            if (wasPassive != nowPassive && !nowPassive) // Only trigger from the one becoming Active
            {
                if (CharacterPlacementManager.Instance != null && CharacterPlacementManager.Instance.IsPairPlaced(card.OwnerID, card.PairID))
                {
                    var cardInfo = card.GetComponent<CardInfo>();
                    if (cardInfo != null && cardInfo.CharData != null)
                    {
                        int libIdx = CharacterPlacementManager.Instance.GetLibraryIndex(cardInfo.CharData);
                        CharacterPlacementManager.Instance.UpdateCharacterModel(card.OwnerID, card.PairID, libIdx);
                    }
                }
            }
        }

        DeckStateChanged?.Invoke(isDeckFull);
    }

    public void MakeActive(CardDragHandler card)
    {
        if (card == null) return;
        
        int p = card.PairID;
        if (p < 0 || p * 2 + 1 >= _cardDeck.Count) return;

        CardSlot activeSlot = _cardDeck[2 * p];
        CardSlot passiveSlot = _cardDeck[2 * p + 1];

        // If the card is already logically in the active slot (or it's its home), do nothing
        if (card.LastSlot == activeSlot) return;

        CardDragHandler partner = card.PartnerCard;
        if (partner != null)
        {
            // If the partner is currently in the active slot, move it to the passive slot
            if (partner.CurrentSlot == activeSlot)
            {
                activeSlot.ClearSlot();
                passiveSlot.SetCardManually(partner);
            }
            else if (partner.LastSlot == activeSlot)
            {
                // If partner is also on field (shouldn't happen with current rules, but safe), just swap their homes
                partner.SetLastSlot(passiveSlot);
            }
        }

        // The card being placed (or made active) should now consider the active slot its home
        card.SetLastSlot(activeSlot);
        
        CheckDeck(); 
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