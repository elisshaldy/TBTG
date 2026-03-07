using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // Added for IPointerClickHandler

public class CardDeckController : MonoBehaviour
{
    public event Action<bool> DeckStateChanged; 

    [SerializeField] private PlayerModel _playerModel;
    
    [SerializeField] private GameDataLibrary _library;
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
            // Змінено: перевіряємо activeSelf замість activeInHierarchy, 
            // бо під час ініціалізації контейнер може бути вимкнений
            while (cardIdx < _cards.Count && (_cards[cardIdx] == null || _cards[cardIdx].CurrentSlot != null || !_cards[cardIdx].gameObject.activeSelf))
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

    public void AutoFillMods()
    {
        List<ModsCardContainer> containers = new List<ModsCardContainer>();
        foreach (var slot in _cardDeck)
        {
            if (slot.IsOccupied && slot.CurrentCard != null)
            {
                var container = slot.CurrentCard.GetComponent<ModsCardContainer>();
                if (container != null) containers.Add(container);
            }
        }

        if (containers.Count == 0) return;

        foreach (var mod in _mods)
        {
            if (mod == null || !mod.gameObject.activeSelf) continue;

            ModInfo info = mod.GetComponent<ModInfo>();
            if (info == null || info.ModData == null) continue;

            foreach (var container in containers)
            {
                if (container.CanAddMod(info.ModData))
                {
                    mod.AttachToCard(container.GetComponent<RectTransform>());
                    break;
                }
            }
        }
    }

    private void CheckDeck()
    {
        bool isDeckFull = true;

        // Iterate by pairs: 0-1, 2-3, 4-5...
        for (int i = 0; i < _cardDeck.Count; i += 2)
        {
            CardSlot activeSlot = _cardDeck[i];
            CardSlot passiveSlot = (i + 1 < _cardDeck.Count) ? _cardDeck[i + 1] : null;

            if (!activeSlot.IsOccupied) isDeckFull = false;

            // 1. Update IsPassive flags
            if (activeSlot.IsOccupied)
            {
                activeSlot.CurrentCard.IsPassive = false;
            }
            if (passiveSlot != null && passiveSlot.IsOccupied)
            {
                passiveSlot.CurrentCard.IsPassive = true;
            }

            // 2. FORCE SYNC WITH MAP: The card in the ACTIVE (Even) slot DICTATES the model on field
            if (activeSlot.IsOccupied && CharacterPlacementManager.Instance != null)
            {
                CardDragHandler activeCard = activeSlot.CurrentCard;
                // Double check: if it's placed on the map, ensure it's the right model
                if (CharacterPlacementManager.Instance.IsPairPlaced(activeCard.OwnerID, activeCard.PairID))
                {
                    var cardInfo = activeCard.GetComponent<CardInfo>();
                    if (cardInfo != null && cardInfo.CharData != null)
                    {
                        int libIdx = CharacterPlacementManager.Instance.GetLibraryIndex(cardInfo.CharData);
                        CharacterPlacementManager.Instance.UpdateCharacterModel(activeCard.OwnerID, activeCard.PairID, libIdx);
                    }
                }
            }
            else if (!activeSlot.IsOccupied && passiveSlot != null && !passiveSlot.IsOccupied && CharacterPlacementManager.Instance != null)
            {
                // Both slots empty = Pair possibly in hand/limbo. If they are on map, 
                // we leave them there for now to prevent flicker while dragging.
            }
        }

        DeckStateChanged?.Invoke(isDeckFull);
    }

    public void MakeActive(CardDragHandler card)
    {
        if (card == null) return;

        CardDragHandler partner = card.PartnerCard;
        CardSlot activeSlot = null;
        CardSlot passiveSlot = null;

        // Знаходимо, яку пару слотів зараз займає ця пара карток
        if (card.CurrentSlot != null)
        {
            int slotIdx = _cardDeck.IndexOf(card.CurrentSlot);
            int pairIdx = slotIdx / 2;
            activeSlot = _cardDeck[pairIdx * 2];
            passiveSlot = _cardDeck[pairIdx * 2 + 1];
        }
        else if (partner != null && partner.CurrentSlot != null)
        {
            int slotIdx = _cardDeck.IndexOf(partner.CurrentSlot);
            int pairIdx = slotIdx / 2;
            activeSlot = _cardDeck[pairIdx * 2];
            passiveSlot = _cardDeck[pairIdx * 2 + 1];
        }
        else if (card.LastSlot != null)
        {
            int slotIdx = _cardDeck.IndexOf(card.LastSlot);
            int pairIdx = slotIdx / 2;
            activeSlot = _cardDeck[pairIdx * 2];
            passiveSlot = _cardDeck[pairIdx * 2 + 1];
        }

        if (activeSlot == null || passiveSlot == null) return;

        // If already correctly placed in its active slot, nothing to do
        if (card.CurrentSlot == activeSlot) return;

        // 1. Properly clear both slots in the pair to avoid "losing" cards or overlapping
        activeSlot.ClearSlot();
        passiveSlot.ClearSlot();

        // 2. Set the requested card to the active slot
        activeSlot.SetCardManually(card);
        card.SetLastSlot(activeSlot);
        
        // 3. Set the partner to the passive slot
        if (partner != null)
        {
            passiveSlot.SetCardManually(partner);
            partner.SetLastSlot(passiveSlot);
        }

        CheckDeck(); 

        if (InitiativeSystem.Instance != null && InitiativeSystem.Instance.IsFinalized)
        {
            InitiativeSystem.Instance.ConsumeAction();
        }
    }

    public CharacterData GetActiveCharacterData(int pairID)
    {
        // Шукаємо у всіх слотах карту з відповідним PairID, яка стоїть в "активній" позиції
        for (int i = 0; i < _cardDeck.Count; i += 2)
        {
            var slot = _cardDeck[i];
            if (slot.IsOccupied && slot.CurrentCard != null && slot.CurrentCard.PairID == pairID)
            {
                var info = slot.CurrentCard.GetComponent<CardInfo>();
                if (info != null) return info.CharData;
            }
        }

        // Фолбек: якщо в слоті порожньо (карта в повітрі), шукаємо активну карту цієї пари в загальному списку
        foreach (var card in _cards)
        {
            if (card != null && card.PairID == pairID && !card.IsPassive)
            {
                var info = card.GetComponent<CardInfo>();
                if (info != null) return info.CharData;
            }
        }
        return null;
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

    /// <summary>
    /// EXCHANGE Action: Replaces the current pair with random characters. 
    /// Consumes 1 action. (Takes effect via CheckDeck inside)
    /// </summary>
    public void ExchangeActivePair()
    {
        if (InitiativeSystem.Instance == null || !InitiativeSystem.Instance.IsFinalized || _library == null) return;

        var activeKey = InitiativeSystem.Instance.GetActiveUnitKey();
        if (activeKey.ownerID == -1) return;

        // Find cards matching this owner and pair
        var pairCards = _cards.FindAll(c => c.OwnerID == activeKey.ownerID && c.PairID == activeKey.pairID);
        var newChars = _library.GetRandomCharacters(pairCards.Count);

        for (int i = 0; i < pairCards.Count; i++)
        {
            var info = pairCards[i].GetComponent<CardInfo>();
            if (info != null && i < newChars.Count)
            {
                info.CharData = newChars[i];
                info.Initialize();
            }
        }

        CheckDeck(); 

        if (InitiativeSystem.Instance != null && InitiativeSystem.Instance.IsFinalized)
        {
            InitiativeSystem.Instance.ConsumeAction();
        }
    }
}