using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class PairFormationManager : MonoBehaviour
{
    [Header("Slot References")]
    public DropSlot[] ActiveSlots;   // 4 зелені комірки
    public DropSlot[] HiddenSlots;   // 4 червоні комірки

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

    /// <summary>
    /// Обробляє початок перетягування картки
    /// </summary>
    public void HandleCardBeginDrag(CardSelectionHandler card)
    {
        // Можна додати звук або візуальний ефект
        Debug.Log($"Started dragging: {card.CardData.CharacterName}");
    }

    /// <summary>
    /// Обробляє закінчення перетягування картки
    /// </summary>
    public void HandleCardEndDrag(CardSelectionHandler card)
    {
        // Можна додати звук або візуальний ефект
        Debug.Log($"Stopped dragging: {card.CardData.CharacterName}");
    }

    /// <summary>
    /// Обробляє скидання картки в слот
    /// </summary>
    public void HandleCardDropped(CardSelectionHandler card, DropSlot slot)
    {
        Debug.Log($"HandleCardDropped: {card.CardData.CharacterName} -> {slot.name}");

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

    /// <summary>
    /// Видаляє картку з усіх слотів крім поточного
    /// </summary>
    private void ClearCardFromAllSlots(CardSelectionHandler card)
    {
        foreach (var slot in GetAllSlots())
        {
            if (slot.ContainsCard(card) && slot != _cardSlotAssignments.GetValueOrDefault(card))
            {
                slot.ClearCardWithoutReturning();
            }
        }
    }

    /// <summary>
    /// Перевіряє завершеність всіх пар
    /// </summary>
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

                Debug.Log($"Pair {pairIndex} completed: {pair.ActiveCharacter.CharacterName} + {pair.HiddenCharacter.CharacterName}");
            }
        }

        Debug.Log($"Completed pairs: {_formedPairs.Count}/4");

        // ВИКЛИК ПОДІЇ ОНОВЛЕННЯ ПАР
        OnPairsUpdated?.Invoke();
    }

    /// <summary>
    /// Оновлює стан кнопки підтвердження
    /// </summary>
    private void UpdateConfirmButtonState()
    {
        if (ConfirmSelectionButton != null)
        {
            bool allPairsComplete = _formedPairs.Count == 4;
            ConfirmSelectionButton.SetActive(allPairsComplete);

            if (allPairsComplete)
            {
                Debug.Log("All 4 pairs completed! Ready to confirm.");
            }
        }
    }

    /// <summary>
    /// Отримати слот за типом і індексом
    /// </summary>
    private DropSlot GetSlot(SlotType type, int pairIndex)
    {
        var slots = (type == SlotType.Active) ? ActiveSlots : HiddenSlots;
        return slots.FirstOrDefault(slot => slot.PairIndex == pairIndex);
    }

    /// <summary>
    /// Отримати всі слоти
    /// </summary>
    private DropSlot[] GetAllSlots()
    {
        return ActiveSlots.Concat(HiddenSlots).ToArray();
    }

    /// <summary>
    /// Викликається при натисканні кнопки підтвердження
    /// </summary>
    public void ConfirmPairs()
    {
        if (_formedPairs.Count != 4)
        {
            Debug.LogWarning("Not all pairs are completed!");
            return;
        }

        Debug.Log("All pairs confirmed! Moving to next phase.");

        // Тут передаємо сформовані пари в іншу систему
        if (PlayerCardManager.Instance != null)
        {
            PlayerCardManager.Instance.DisplayFormedPairs(_formedPairs);
        }

        // Вимкнути драг & дроп після підтвердження
        SetDragDropEnabled(false);
    }

    /// <summary>
    /// Увімкнути/вимкнути драг & дроп
    /// </summary>
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

    /// <summary>
    /// Скинути всі пари (для рестарту)
    /// </summary>
    public void ResetAllPairs()
    {
        foreach (var slot in GetAllSlots())
        {
            slot.RemoveCard();
        }

        _formedPairs.Clear();
        _cardSlotAssignments.Clear();
        UpdateConfirmButtonState();

        Debug.Log("All pairs reset.");
    }

    // Гетери для перевірки стану
    public int GetCompletedPairsCount() => _formedPairs.Count;
    public List<CharacterPair> GetFormedPairs() => new List<CharacterPair>(_formedPairs);
}