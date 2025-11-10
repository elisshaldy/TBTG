// GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dependencies")]
    public GridManager GridManager;
    public InitiativeManager InitiativeManager;

    [Header("Players")]
    public PlayerController Player1;
    public PlayerController Player2;

    [Header("Game State")]
    public int CurrentRound = 0;
    private PlayerController _activePlayer;

    [Header("Placement State")]
    private Queue<PlayerController> _placementOrder;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (GridManager == null) GridManager = FindObjectOfType<GridManager>();
        if (InitiativeManager == null) InitiativeManager = FindObjectOfType<InitiativeManager>();
    }

    void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        Player1.Initialize(1, this);
        Player2.Initialize(2, this);

        Debug.Log("Game Initialized. Starting Draft Phase (assuming Draft Manager starts it).");
        // Примітка: Логіку запуску драфту тут пропущено, оскільки вона, ймовірно, 
        // знаходиться в іншому ініціалізаторі або в самому GameDeckManager.
    }

    // ----------------------------------------------------------------------
    // МЕТОД ІНТЕГРАЦІЇ: ВИПРАВЛЕННЯ CS1061. 
    // Викликається GameDeckManager після завершення драфту.
    // ----------------------------------------------------------------------
    /// <summary>
    /// Викликається DeckManager, коли гравець підтвердив свій вибір карт.
    /// Переходить до фази розміщення.
    /// </summary>
    public void CompleteDraftPhase()
    {
        Debug.Log("Draft Phase successfully completed. Moving to Placement Phase (Setup Phase).");

        // Тут PlayerController вже повинен мати доступ до обраних карт через PlayerHandData.
        StartSetupPhase();
    }
    // ----------------------------------------------------------------------


    // ----------------------------------------------------------------------
    // ДОПОМІЖНИЙ МЕТОД: Визначення зон розміщення (3x3 протилежні кути)
    // ----------------------------------------------------------------------
    public List<Vector2Int> GetPlayerPlacementZone(int playerID)
    {
        List<Vector2Int> zone = new List<Vector2Int>();

        // P1: Нижній Лівий Кут (0,0 до 2,2)
        if (playerID == 1)
        {
            for (int x = 0; x <= 2; x++)
                for (int y = 0; y <= 2; y++)
                    zone.Add(new Vector2Int(x, y));
        }
        // P2: Верхній Правий Кут (6,6 до 8,8) - протилежний
        else if (playerID == 2)
        {
            for (int x = 6; x <= 8; x++)
                for (int y = 6; y <= 8; y++)
                    zone.Add(new Vector2Int(x, y));
        }
        return zone;
    }

    // ----------------------------------------------------------------------
    // ФАЗА 1: Розміщення Персонажів (Перед першим раундом)
    // ----------------------------------------------------------------------
    public void StartSetupPhase()
    {
        // Ініціалізація черги розміщення (Hot Seat: P1, P2, P1, P2...)
        _placementOrder = new Queue<PlayerController>();

        int maxChars = Mathf.Max(Player1.CharactersToPlaceList.Count, Player2.CharactersToPlaceList.Count);

        for (int i = 0; i < maxChars; i++)
        {
            if (i < Player1.CharactersToPlaceList.Count) _placementOrder.Enqueue(Player1);
            if (i < Player2.CharactersToPlaceList.Count) _placementOrder.Enqueue(Player2);
        }

        Debug.Log($"Setup Phase: Total {_placementOrder.Count} placement turns. Starting.");

        StartNextPlacementTurn();
    }

    private void StartNextPlacementTurn()
    {
        if (_placementOrder.Count == 0)
        {
            Debug.Log("All characters placed. Setup Phase complete.");
            StartRound(1); // Перехід до першого раунду
            return;
        }

        _activePlayer = _placementOrder.Dequeue();

        List<Vector2Int> availableZone = GetPlayerPlacementZone(_activePlayer.PlayerID);
        _activePlayer.StartPlacement(availableZone);
    }

    // Викликається PlayerController після того, як він розмістив одного персонажа
    public void CompletePlacementTurn()
    {
        StartNextPlacementTurn();
    }

    // ----------------------------------------------------------------------
    // ФАЗА 2: Ініціатива (На початку кожного раунду)
    // ----------------------------------------------------------------------
    public void StartRound(int roundNumber)
    {
        CurrentRound = roundNumber;

        Player1.DeckManager.ReplenishHand();
        Player2.DeckManager.ReplenishHand();

        // Передача керування InitiativeManager
        InitiativeManager.StartInitiativePhase(Player1.ActiveCharacters, Player2.ActiveCharacters);

        Debug.Log($"Round {CurrentRound} started. Initiative planning phase.");

        SimulateInitiativeSetup();
    }

    private void SimulateInitiativeSetup()
    {
        List<InitiativeToken> fakeTrack = new List<InitiativeToken>();

        List<Character> p1 = Player1.ActiveCharacters.OrderBy(x => Random.value).ToList();
        List<Character> p2 = Player2.ActiveCharacters.OrderBy(x => Random.value).ToList();

        int totalMoves = p1.Count + p2.Count;

        for (int i = 0; i < totalMoves; i++)
        {
            if (i % 2 == 0 && i < p1.Count)
            {
                fakeTrack.Add(new InitiativeToken(p1[i], Player1.PlayerID));
            }
            else if (i < p2.Count)
            {
                fakeTrack.Add(new InitiativeToken(p2[i], Player2.PlayerID));
            }
        }

        InitiativeManager.SetInitiativeOrder(fakeTrack);
    }

    // ----------------------------------------------------------------------
    // ФАЗА 3: Виконання Ходу
    // ----------------------------------------------------------------------
    public void StartCharacterTurn(InitiativeToken token)
    {
        _activePlayer = (token.PlayerID == Player1.PlayerID) ? Player1 : Player2;

        Debug.Log($"Hot Seat: Passing control to Player {_activePlayer.PlayerID} to move {token.CharacterReference.Data.CharacterName}.");

        _activePlayer.StartTurn(token.CharacterReference);
    }

    public void EndCharacterTurn()
    {
        InitiativeManager.CompleteTurn();
    }

    // ----------------------------------------------------------------------
    // ФАЗА 4: Кінець Раунду
    // ----------------------------------------------------------------------
    public void EndRound()
    {
        Debug.Log("Round execution finished. Starting next round...");
        StartRound(CurrentRound + 1);
    }
}