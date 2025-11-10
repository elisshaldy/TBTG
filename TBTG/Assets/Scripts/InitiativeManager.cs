// InitiativeManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InitiativeManager : MonoBehaviour
{
    public static InitiativeManager Instance { get; private set; }

    // Черга токенів, що визначає порядок ходів на раунд
    private List<InitiativeToken> _initiativeTrack = new List<InitiativeToken>();

    // Список усіх активних персонажів обох гравців
    private List<Character> _allActiveCharacters = new List<Character>();

    // Поточний токен, чий хід виконується
    private int _currentTokenIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ----------------------------------------------------------------------
    // ФАЗА ПЛАНУВАННЯ
    // ----------------------------------------------------------------------

    // Викликається на початку раунду для підготовки
    public void StartInitiativePhase(List<Character> player1Characters, List<Character> player2Characters)
    {
        _initiativeTrack.Clear();
        _allActiveCharacters.Clear();
        _allActiveCharacters.AddRange(player1Characters);
        _allActiveCharacters.AddRange(player2Characters);
        _currentTokenIndex = 0;

        // Тут має бути логіка UI, де гравці таємно виставляють свої токени.
        // Це асинхронний процес, який завершується викликом `SetInitiativeOrder`.

        Debug.Log("Initiative Phase Started. Waiting for players to set tokens.");
    }

    // Метод, який викликається після того, як обидва гравці завершили таємне виставлення
    public void SetInitiativeOrder(List<InitiativeToken> combinedTokensInOrder)
    {
        // 1. Спочатку визначаємо, хто ходить першим (закид кубика, як у GDD)
        // Для спрощення: припустимо, черговість вже визначена у 'combinedTokensInOrder'

        _initiativeTrack = combinedTokensInOrder;

        Debug.Log("Initiative order set. Total moves this round: " + _initiativeTrack.Count);

        // Почати виконання раунду
        StartRoundExecution();
    }

    // ----------------------------------------------------------------------
    // ФАЗА ВИКОНАННЯ
    // ----------------------------------------------------------------------

    public void StartRoundExecution()
    {
        if (_initiativeTrack.Count > 0)
        {
            ExecuteNextTurn();
        }
        else
        {
            EndRound();
        }
    }

    public void ExecuteNextTurn()
    {
        if (_currentTokenIndex >= _initiativeTrack.Count)
        {
            EndRound();
            return;
        }

        InitiativeToken currentToken = _initiativeTrack[_currentTokenIndex];
        currentToken.IsRevealed = true; // Відкриваємо токен (показуємо, хто ходить)

        Debug.Log($"Turn {_currentTokenIndex + 1}: Player {currentToken.PlayerID} moves {currentToken.CharacterReference.Data.CharacterName}");

        // Тут потрібно повідомити PlayerController (контролер гравця), що його черга
        // Наприклад: PlayerController.Instance.StartCharacterTurn(currentToken.CharacterReference);

        // Після того, як PlayerController повідомить, що хід завершено (через CompleteTurn()), 
        // викликається NextTurn().
    }

    // Викликається після того, як поточний персонаж завершив свої дві дії
    public void CompleteTurn()
    {
        _currentTokenIndex++;
        ExecuteNextTurn();
    }

    private void EndRound()
    {
        Debug.Log("Round Ended. Starting next Initiative Phase.");
        // Тут логіка: оновлення активних клітинок, добір карт руху, перехід до нової фази ініціативи
        // GameManager.Instance.AdvanceToNextRound();
    }
}