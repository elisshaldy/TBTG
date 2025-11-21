// MovementDeckManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MovementDeckManager : MonoBehaviour
{
    [Header("Deck Settings")]
    public int MaxHandSize = 6;
    public List<MovementCardData> MasterDeck; // ����� ������, � ��� ������ �����

    [Header("Player State")]
    public List<MovementCardData> CurrentHand = new List<MovementCardData>();
    private List<MovementCardData> _discardPile = new List<MovementCardData>();
    private List<MovementCardData> _burnedCards = new List<MovementCardData>();

    /// <summary>
    /// Поточна кількість карт у руці (для перевірок штрафу за пасивність).
    /// </summary>
    public int CurrentHandCount => CurrentHand.Count;

    // ----------------------------------------------------------------------
    // ������� ��ò�� ������ ����
    // ----------------------------------------------------------------------

    public void Start()
    {
        MovementDeckManager player1Deck = GetComponent<MovementDeckManager>();
        player1Deck.DrawInitialHand();
    }

    public void DrawInitialHand()
    {
        // �� ������� ��� �������� 6 ����
        DrawCards(MaxHandSize);
    }

    public void ReplenishHand()
    {
        // �� ������� ������ �������� ������, ��� ������� MaxHandSize (���� ���� ������)
        DrawCards(MaxHandSize - CurrentHand.Count);
    }

    private void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (CurrentHand.Count >= MaxHandSize) return;

            // ���� ������ �������, ��������� ����
            if (MasterDeck.Count == 0)
            {
                if (_discardPile.Count == 0)
                {
                    Debug.LogWarning("Deck and Discard are empty. Cannot draw.");
                    break;
                }
                ShuffleDiscardIntoDeck();
            }

            // ������ ������ �����
            MovementCardData card = MasterDeck[0];
            MasterDeck.RemoveAt(0);
            CurrentHand.Add(card);
        }
    }

    private void ShuffleDiscardIntoDeck()
    {
        // ������������ Fisher-Yates
        MasterDeck.AddRange(_discardPile);
        _discardPile.Clear();
        MasterDeck = MasterDeck.OrderBy(x => Random.value).ToList();
        Debug.Log("Discard shuffled into Deck.");
    }

    // ----------------------------------------------------------------------
    // ��ò�� ������������
    // ----------------------------------------------------------------------

    public void UseCard(MovementCardData card, bool burnCard)
    {
        if (CurrentHand.Contains(card))
        {
            CurrentHand.Remove(card);

            if (burnCard)
            {
                // ��� ��������� ����
                _burnedCards.Add(card);
                Debug.Log($"Card {card.CardName} was BURNED and removed from game pool.");
            }
            else
            {
                // �������� ������������
                _discardPile.Add(card);
            }
        }
    }

    /// <summary>
    /// Отримати випадкову картку з руки (використовується для штрафу за пасивність,
    /// якщо немає UI для вибору суперником конкретної картки).
    /// </summary>
    public MovementCardData GetRandomCardFromHand()
    {
        if (CurrentHand.Count == 0) return null;
        int index = Random.Range(0, CurrentHand.Count);
        return CurrentHand[index];
    }

    /// <summary>
    /// Видалити конкретну картку з руки без додаткових ефектів.
    /// </summary>
    public void RemoveCardFromHand(MovementCardData card)
    {
        if (card != null && CurrentHand.Contains(card))
        {
            CurrentHand.Remove(card);
        }
    }

    /// <summary>
    /// Прийняти картку суперника в результаті штрафу за пасивність.
    /// Якщо є місце в руці — додаємо в руку, інакше скидаємо у відбій.
    /// </summary>
    public void ReceivePenaltyCard(MovementCardData card)
    {
        if (card == null) return;

        if (CurrentHand.Count < MaxHandSize)
        {
            CurrentHand.Add(card);
            Debug.Log($"Received penalty card '{card.CardName}' into hand.");
        }
        else
        {
            _discardPile.Add(card);
            Debug.Log($"Received penalty card '{card.CardName}' into discard (hand is full).");
        }
    }

    // ----------------------------------------------------------------------
    // ��ò�� ����Բ� �� ������� ��������
    // ----------------------------------------------------------------------

    public void ApplyInactivityPenalty(MovementCardData cardToLose)
    {
        if (CurrentHand.Contains(cardToLose))
        {
            CurrentHand.Remove(cardToLose);
            // ����� ��� � ���� ������ (���� ������� ������ ������ ��������)
            _discardPile.Add(cardToLose);
            Debug.Log("Inactivity penalty applied. Card discarded.");
        }
    }

    public void RestoreHandOnPairSwap(List<MovementCardData> newRandomCards)
    {
        // ��������������� ��� ���������� ��������� ����� ����
        CurrentHand.Clear();
        _burnedCards.Clear(); // ��������� ��������

        // �� ���� ����� ����� � ���� (��� MasterDeck)
        _discardPile.AddRange(CurrentHand);

        // ������ ��� �������� �����
        CurrentHand.AddRange(newRandomCards.Take(MaxHandSize));

        Debug.Log("Hand restored and Burned cards reset due to Random Pair Swap.");
    }
}