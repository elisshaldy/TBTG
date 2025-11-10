// MovementDeckManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MovementDeckManager : MonoBehaviour
{
    [Header("Deck Settings")]
    public int MaxHandSize = 6;
    public List<MovementCardData> MasterDeck; // Повна колода, з якої беремо карти

    [Header("Player State")]
    public List<MovementCardData> CurrentHand = new List<MovementCardData>();
    private List<MovementCardData> _discardPile = new List<MovementCardData>();
    private List<MovementCardData> _burnedCards = new List<MovementCardData>();

    // ----------------------------------------------------------------------
    // ОСНОВНА ЛОГІКА ДОБОРА КАРТ
    // ----------------------------------------------------------------------

    public void Start()
    {
        MovementDeckManager player1Deck = GetComponent<MovementDeckManager>();
        player1Deck.DrawInitialHand();
    }

    public void DrawInitialHand()
    {
        // На початку гри добираємо 6 карт
        DrawCards(MaxHandSize);
    }

    public void ReplenishHand()
    {
        // На початку раунду добираємо стільки, щоб досягти MaxHandSize (якщо немає штрафу)
        DrawCards(MaxHandSize - CurrentHand.Count);
    }

    private void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (CurrentHand.Count >= MaxHandSize) return;

            // Якщо колода порожня, перемішуємо відбій
            if (MasterDeck.Count == 0)
            {
                if (_discardPile.Count == 0)
                {
                    Debug.LogWarning("Deck and Discard are empty. Cannot draw.");
                    break;
                }
                ShuffleDiscardIntoDeck();
            }

            // Беремо верхню карту
            MovementCardData card = MasterDeck[0];
            MasterDeck.RemoveAt(0);
            CurrentHand.Add(card);
        }
    }

    private void ShuffleDiscardIntoDeck()
    {
        // Перемішування Fisher-Yates
        MasterDeck.AddRange(_discardPile);
        _discardPile.Clear();
        MasterDeck = MasterDeck.OrderBy(x => Random.value).ToList();
        Debug.Log("Discard shuffled into Deck.");
    }

    // ----------------------------------------------------------------------
    // ЛОГІКА ВИКОРИСТАННЯ
    // ----------------------------------------------------------------------

    public void UseCard(MovementCardData card, bool burnCard)
    {
        if (CurrentHand.Contains(card))
        {
            CurrentHand.Remove(card);

            if (burnCard)
            {
                // Для подвійного руху
                _burnedCards.Add(card);
                Debug.Log($"Card {card.CardName} was BURNED and removed from game pool.");
            }
            else
            {
                // Звичайне використання
                _discardPile.Add(card);
            }
        }
    }

    // ----------------------------------------------------------------------
    // ЛОГІКА ШТРАФІВ ТА ПОВНОГО СКИНАННЯ
    // ----------------------------------------------------------------------

    public void ApplyInactivityPenalty(MovementCardData cardToLose)
    {
        if (CurrentHand.Contains(cardToLose))
        {
            CurrentHand.Remove(cardToLose);
            // Карта йде у відбій гравця (якщо опонент вирішив просто викинути)
            _discardPile.Add(cardToLose);
            Debug.Log("Inactivity penalty applied. Card discarded.");
        }
    }

    public void RestoreHandOnPairSwap(List<MovementCardData> newRandomCards)
    {
        // Використовується при одноразовій рандомній заміні пари
        CurrentHand.Clear();
        _burnedCards.Clear(); // Обнулення вигорань

        // Всі старі карти йдуть у відбій (для MasterDeck)
        _discardPile.AddRange(CurrentHand);

        // Додаємо нові випадкові карти
        CurrentHand.AddRange(newRandomCards.Take(MaxHandSize));

        Debug.Log("Hand restored and Burned cards reset due to Random Pair Swap.");
    }
}