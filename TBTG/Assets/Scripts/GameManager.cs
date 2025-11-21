// GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static GameManager Instance { get; private set; }

    // --- DEPENDENCIES ---
    [Header("Dependencies")]
    public GridManager GridManager;
    public InitiativeManager InitiativeManager;

    // --- PLAYERS ---
    [Header("Players")]
    public PlayerController Player1;
    public PlayerController Player2;

    // --- UI REFERENCES ---
    [Tooltip("������ UI, �� �������� ����� �� ��� �������� ���� (��� Hot-Seat).")]
    public GameObject HandoverPanel;

    // --- GAME STATE ---
    [Header("Game State")]
    public int CurrentRound = 0;
    private PlayerController _activePlayer;

    // --- PLACEMENT STATE ---
    [Header("Placement State")]
    private Queue<PlayerController> _placementOrder;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // ����� �����������, ���� �� ����������
        if (GridManager == null) GridManager = FindObjectOfType<GridManager>();
        if (InitiativeManager == null) InitiativeManager = FindObjectOfType<InitiativeManager>();
    }

    void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        // ������������ ���������� �������
        Player1.Initialize(1, this);
        Player2.Initialize(2, this);

        // �������������, �� ������ ��������� ��� �����
        if (HandoverPanel != null) HandoverPanel.SetActive(false);

        Debug.Log("Game Initialized. Starting Draft Phase.");
        // ��� �� ���� ������ StartDraftPhase() � GameDeckManager

        // !!! ��� ����������: ����� ��������� PLACEMENT PHASE ²����� !!!
        // StartPlacementPhase();
    }

    // ----------------------------------------------------------------------
    // ���� 1: ��������� (Placement)
    // ----------------------------------------------------------------------

    /// <summary>
    /// ������ ���� ��������� ���������, ���������� �����.
    /// </summary>
    public void StartPlacementPhase()
    {
        CurrentRound = 1;
        _placementOrder = new Queue<PlayerController>(new[] { Player1, Player2 }); // �� ����

        // �������: ���������� ��� ��������� (��� ����� ���� ������� ����������)
        List<Vector2Int> zone1 = new List<Vector2Int>();
        List<Vector2Int> zone2 = new List<Vector2Int>();

        // ������������ ���� � PlayerController (����� �� ����ު!)
        Player1.SetPlacementZone(zone1);
        Player2.SetPlacementZone(zone2);

        StartNextPlacementTurn();
    }

    /// <summary>
    /// ������� ��� ��������� ��� ���������� ������.
    /// </summary>
    public void StartNextPlacementTurn() // �������� ��������, ��� ��������� � PlayerController
    {
        if (_placementOrder.Count > 0)
        {
            PlayerController nextPlayer = _placementOrder.Dequeue();

            // !!! ���������� ������ !!!
            nextPlayer.StartPlacement(); // ��������� StartPlacement ��� �������� ����
        }
        else
        {
            Debug.Log("Placement Phase finished. Starting Initiative Phase.");
            // StartInitiativePhase();
        }
    }

    /// <summary>
    /// ������� ���� ��������� ��� PlayerController (�������� �� GridManager).
    /// ��� ����� ��� ��������� ��� ������ �������, ���� ���� ��� ����������� � SetPlacementZone.
    /// </summary>
    public List<Vector2Int> GetPlayerPlacementZone(int playerID)
    {
        // �� ��������� ���, ���� �� ���� ����������� �� ���� GridManager
        if (playerID == 1)
        {
            // ��������� ���� ������ 1
            return new List<Vector2Int> { /* ... */ };
        }
        else if (playerID == 2)
        {
            // ��������� ���� ������ 2
            return new List<Vector2Int> { /* ... */ };
        }
        return new List<Vector2Int>();
    }

    // ----------------------------------------------------------------------
    // ���� 3: ��������� ���� (� ����Բ��ֲ��� ��� HOT-SEAT)
    // ----------------------------------------------------------------------

    /// <summary>
    /// ����������� InitiativeManager, ���� ���������� ��� ���������.
    /// ����� �������� ���� ������� Handover.
    /// </summary>
    public void StartCharacterTurn(InitiativeToken token)
    {
        // ��������� ��������� ������
        _activePlayer = (token.PlayerID == Player1.PlayerID) ? Player1 : Player2;

        Debug.Log($"Hot Seat: Passing control to Player {_activePlayer.PlayerID} to move {token.CharacterReference.Data.CharacterName}.");

        // !!! ����: ������� ��ò�� ������ײ ���� !!!
        StartHandover(_activePlayer, token.CharacterReference);
    }

    /// <summary>
    /// ������ ���� ���������� ������ ��� �������� ��������.
    /// </summary>
    private void StartHandover(PlayerController nextPlayer, Character characterToMove)
    {
        if (HandoverPanel == null)
        {
            Debug.LogError("HandoverPanel �� ����������! ��������� ��� �������.");
            nextPlayer.StartTurn(characterToMove);
            return;
        }

        // ³��������� ������
        HandoverPanel.SetActive(true);

        // ��������� ������ �� �����, ��� ����'����� �� �� �����
        Button continueButton = HandoverPanel.GetComponentInChildren<Button>();
        if (continueButton != null)
        {
            // ������� ���� ������� �� ������ �����
            continueButton.onClick.RemoveAllListeners();
            // ������������� �������-�������� ��� �������� ���������
            continueButton.onClick.AddListener(() => CompleteHandover(nextPlayer, characterToMove));
        }
        else
        {
            Debug.LogError("�� HandoverPanel �������� ��������� Button! ������� ����, ��� ������� �� ����������.");
            // ���� ���� ������, �� �� ������ ����������� ����������. ճ� ������������.
        }
    }

    /// <summary>
    /// ������� ���� ���������� � ������ ��� ������.
    /// </summary>
    public void CompleteHandover(PlayerController nextPlayer, Character characterToMove)
    {
        // ��������� ������
        if (HandoverPanel != null)
        {
            HandoverPanel.SetActive(false);

            // ������� ������� ������
            Button continueButton = HandoverPanel.GetComponentInChildren<Button>();
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
            }
        }

        // �������� ��������� ��� ������
        nextPlayer.StartTurn(characterToMove);

        Debug.Log($"Handover completed. Player {nextPlayer.PlayerID} is now active.");
    }


    /// <summary>
    /// ����������� PlayerController, ���� �������� �������� ��� 䳿.
    /// </summary>
    public void EndCharacterTurn()
    {
        // ����������� InitiativeManager, �� ��� ���������
        InitiativeManager.CompleteTurn();
    }

    // ----------------------------------------------------------------------
    // ���� 4: ʳ���� ������
    // ----------------------------------------------------------------------
    public void EndRound()
    {
        // ����� ���� ������:
        // 1. ������� ������ (Active Tiles)
        // 2. ��������� ����� ���� (MovementDeckManager.ReplenishHand)
        // 3. ��������� ��������� ������

        // 1. Обробка ефектів Active Tiles, які лікують / погіршують стан персонажів
        if (GridManager != null)
        {
            foreach (var coords in GridManager.GetAllRegisteredCoords())
            {
                Tile tile = GridManager.GetTile(coords);
                if (tile == null || tile.Type != TileType.ActiveTile) continue;
                if (tile.Occupant == null || tile.CurrentEffect == null) continue;

                // Лікування / погіршення стану згідно з ефектом клітинки
                if (tile.CurrentEffect.HealOnRoundEnd)
                {
                    tile.Occupant.ApplyHealing(1);
                }

                if (tile.CurrentEffect.WorsenStateOnRoundEnd)
                {
                    tile.Occupant.ApplyDamage(1);
                }
            }
        }

        // 2. Штраф за пасивність для кожного гравця (спрощена реалізація без вибору карти через UI):
        // якщо в кінці раунду карт у руці > 2, суперник випадково "забирає" одну картку
        // (якщо в нього є місце — в руку, інакше у відбій).
        if (Player1 != null && Player2 != null)
        {
            ApplyInactivityPenaltyBetweenPlayers(Player1, Player2);
            ApplyInactivityPenaltyBetweenPlayers(Player2, Player1);
        }

        // 3. Добір карт руху до повного ліміту (якщо потрібно)
        Player1?.DeckManager?.ReplenishHand();
        Player2?.DeckManager?.ReplenishHand();

        CurrentRound++;
        Debug.Log($"Round {CurrentRound - 1} ended. Starting Round {CurrentRound} Initiative Phase.");

        // StartInitiativePhase();
    }

    /// <summary>
    /// Застосувати штраф за пасивність: якщо у гравця-бенефіціара (offender) наприкінці раунду
    /// більше 2 карт руху в руці, суперник випадково забирає одну картку.
    /// </summary>
    private void ApplyInactivityPenaltyBetweenPlayers(PlayerController offender, PlayerController opponent)
    {
        if (offender == null || opponent == null) return;
        if (offender.DeckManager == null || opponent.DeckManager == null) return;

        var offenderDeck = offender.DeckManager;
        var opponentDeck = opponent.DeckManager;

        if (offenderDeck.CurrentHandCount > 2)
        {
            MovementCardData cardToLose = offenderDeck.GetRandomCardFromHand();
            if (cardToLose == null) return;

            offenderDeck.RemoveCardFromHand(cardToLose);
            opponentDeck.ReceivePenaltyCard(cardToLose);

            Debug.Log($"Inactivity penalty: Player {offender.PlayerID} loses card '{cardToLose.CardName}' to Player {opponent.PlayerID}.");
        }
    }
}