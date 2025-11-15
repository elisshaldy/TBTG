using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class PairFormationManager : MonoBehaviour
{
    [Header("Slot References")]
    public DropSlot[] ActiveSlots;
    public DropSlot[] HiddenSlots;

    [Header("UI References")]
    public Transform DraftCardsContainer;
    public GameObject ConfirmSelectionButton;

    [Header("Events")]
    public UnityEvent OnPairsUpdated;

    private List<CharacterPair> _formedPairs = new List<CharacterPair>();
    private Dictionary<CardSelectionHandler, DropSlot> _cardSlotAssignments = new Dictionary<CardSelectionHandler, DropSlot>();

    public static PairFormationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Підписка на події всіх слотів
        foreach (var slot in GetAllSlots())
        {
            // Можна додати додаткову логіку ініціалізації
        }
    }

    public void HandleCardBeginDrag(CardSelectionHandler card)
    {
        Debug.Log($"Started dragging: {card.CardData.CharacterName}");
    }

    public void HandleCardEndDrag(CardSelectionHandler card)
    {
        Debug.Log($"Stopped dragging: {card.CardData.CharacterName}");
    }

    public void HandleCardDropped(CardSelectionHandler card, DropSlot slot)
    {
        // Очищаємо картку з будь-якого іншого слота
        ClearCardFromAllSlots(card);

        // Оновлюємо призначення
        if (_cardSlotAssignments.ContainsKey(card))
        {
            _cardSlotAssignments[card] = slot;
        }
        else
        {
            _cardSlotAssignments.Add(card, slot);
        }

        CheckPairsCompletion();
        UpdateConfirmButtonState();
    }

    public void HandleCardReturnedToDraft(CardSelectionHandler card)
    {
        // Видаляємо картку з призначень
        if (_cardSlotAssignments.ContainsKey(card))
        {
            _cardSlotAssignments.Remove(card);
        }

        // Очищаємо картку з усіх слотів
        ClearCardFromAllSlots(card);

        CheckPairsCompletion();
        UpdateConfirmButtonState();

        Debug.Log($"Card {card.CardData.CharacterName} returned to draft and removed from pairs");
    }

    private void ClearCardFromAllSlots(CardSelectionHandler card)
    {
        foreach (var slot in GetAllSlots())
        {
            if (slot.ContainsCard(card))
            {
                slot.ClearCardWithoutReturning();
            }
        }
    }

    private void CheckPairsCompletion()
    {
        _formedPairs.Clear();

        for (int pairIndex = 0; pairIndex < 4; pairIndex++)
        {
            DropSlot activeSlot = GetSlot(SlotType.Active, pairIndex);
            DropSlot hiddenSlot = GetSlot(SlotType.Hidden, pairIndex);

            if (activeSlot.IsOccupied() && hiddenSlot.IsOccupied())
            {
                CharacterPair pair = new CharacterPair(activeSlot.GetCardData(), hiddenSlot.GetCardData());
                _formedPairs.Add(pair);
            }
        }

        OnPairsUpdated?.Invoke();
    }

    private void UpdateConfirmButtonState()
    {
        if (ConfirmSelectionButton != null)
        {
            bool allPairsComplete = _formedPairs.Count == 4;
            ConfirmSelectionButton.SetActive(allPairsComplete);
        }
    }

    private DropSlot GetSlot(SlotType type, int pairIndex)
    {
        var slots = (type == SlotType.Active) ? ActiveSlots : HiddenSlots;
        return slots.FirstOrDefault(slot => slot.PairIndex == pairIndex);
    }

    private DropSlot[] GetAllSlots()
    {
        return ActiveSlots.Concat(HiddenSlots).ToArray();
    }

    public void ConfirmPairs()
    {
        if (_formedPairs.Count != 4)
        {
            Debug.LogWarning("Not all pairs are completed!");
            return;
        }

        Debug.Log("All pairs confirmed! Moving to next phase.");

        if (PlayerCardManager.Instance != null)
        {
            PlayerCardManager.Instance.DisplayFormedPairs(_formedPairs);
        }

        SetDragDropEnabled(false);
    }

    public void SetDragDropEnabled(bool enabled)
    {
        foreach (var card in FindObjectsOfType<CardSelectionHandler>())
        {
            card.enabled = enabled;
        }

        foreach (var slot in GetAllSlots())
        {
            slot.enabled = enabled;
        }
    }

    public void ResetAllPairs()
    {
        foreach (var slot in GetAllSlots())
        {
            slot.RemoveCard();
        }

        _formedPairs.Clear();
        _cardSlotAssignments.Clear();
        UpdateConfirmButtonState();
    }

    public int GetCompletedPairsCount() => _formedPairs.Count;
    public List<CharacterPair> GetFormedPairs() => new List<CharacterPair>(_formedPairs);
}