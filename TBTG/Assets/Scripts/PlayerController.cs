// PlayerController.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    [Header("Configuration")]
    public int PlayerID;
    public PlacementInputHandler InputHandler; // ���������� � ���������

    [Header("Placement State")]
    // ��������� ��� ������ � ��������� �� ������� ���
    public List<Character> CharactersToPlaceList;
    private Queue<Character> _charactersToPlaceQueue;

    public List<Character> ActiveCharacters = new List<Character>();

    [Header("Dependencies")]
    public MovementDeckManager DeckManager;

    [Header("Turn State")]
    public Character CurrentCharacter;
    private int _actionsRemaining = 2;
    private GameManager _gameManager;
    private List<Vector2Int> _currentPlacementZone; // ����, ����������� GameManager

    // Службові прапорці для логіки ходу
    private bool _hasMovedOnceThisTurn = false;
    private bool _lastActionWasAttack = false;

    public void Initialize(int id, GameManager manager)
    {
        PlayerID = id;
        _gameManager = manager;

        _charactersToPlaceQueue = new Queue<Character>(CharactersToPlaceList);

        if (DeckManager == null) DeckManager = gameObject.AddComponent<MovementDeckManager>();
        DeckManager.DrawInitialHand();
    }

    // ----------------------------------------------------------------------
    // !!! ����������� CS1061: ������ ����� !!!
    // ----------------------------------------------------------------------
    /// <summary>
    /// ���������� ��������� ���� ��������� ��� ������.
    /// ����������� GameManager �� ������� Placement Phase.
    /// </summary>
    public void SetPlacementZone(List<Vector2Int> zone)
    {
        _currentPlacementZone = zone;
        Debug.Log($"Player {PlayerID} placement zone set. Zone size: {zone.Count}");
    }
    // ----------------------------------------------------------------------
    // ʲ���� �����������
    // ----------------------------------------------------------------------

    // ----------------------------------------------------------------------
    // ���� 1.1: ��������� ���������
    // ----------------------------------------------------------------------
    /// <summary>
    /// ������ ��� ��������� ��� ����� ������.
    /// </summary>
    public void StartPlacement() // ��������: �������� List<Vector2Int> availableZone � ���������
    {
        if (!_charactersToPlaceQueue.Any())
        {
            Debug.Log($"Player {PlayerID} finished placement. Calling EndPlacementPhase.");
            // ��� �� ���� ������ ������ �� GameManager ��� �������� �� ���������� ������
            // ��� ��� ������� EndPlacementPhase
            return;
        }

        Character characterToPlace = _charactersToPlaceQueue.Peek(); // ������ ������ ����������, ��� �� ���������

        Debug.Log($"Player {PlayerID} must place {characterToPlace.Data.CharacterName}.");

        // �������� InputHandler, �������� ���� PlayerController
        // InputHandler ������� �����, �� ���� ����� ������� ��������� _currentPlacementZone
        // �������: �� ������ ������ _currentPlacementZone � StartListening, ���� ������
        InputHandler.StartListening(this);
    }

    /// <summary>
    /// �������� ��������� ��������� �� �������. ����������� InputHandler.
    /// </summary>
    public void PlaceCharacter(Character character, Vector2Int coords)
    {
        if (_charactersToPlaceQueue.Peek() != character)
        {
            Debug.LogWarning("������ ��������� �� ���� ���������. ������� �� � ��������� ����.");
            return;
        }

        _charactersToPlaceQueue.Dequeue(); // ��������� � �����

        // Գ����� ��������� �� ���
        // 1. ��������� ������� Character
        character.GridPosition = coords;

        // 2. ��������� ������� (Tile)
        Tile tile = _gameManager.GridManager.GetTile(coords);
        if (tile != null)
        {
            tile.SetOccupant(character);
            character.transform.position = tile.transform.position; // ���������� ��'����
        }

        ActiveCharacters.Add(character);
        Debug.Log($"P{PlayerID} placed {character.Data.CharacterName} at {coords}.");

        InputHandler.StopListening();

        // ���������� �� ��������� ���������� ���������
        // _gameManager.StartNextPlacementTurn(); // �� �� ���� ��������� GameManager
    }

    // ----------------------------------------------------------------------
    // ���� 3: ��������� ����
    // ----------------------------------------------------------------------

    public void StartTurn(Character character)
    {
        CurrentCharacter = character;
        _actionsRemaining = 2; // ������� ��������� 2 䳿

        // Скидаємо службові прапорці на початку ходу
        _hasMovedOnceThisTurn = false;
        _lastActionWasAttack = false;

        // ��� �� ���� ����� ����������� ����� ���������, ��������� UI ����.

        Debug.Log($"P{PlayerID} turn started with {CurrentCharacter.Data.CharacterName}. Actions left: {_actionsRemaining}");
    }

    // �������: ��� ���������
    public void PerformMoveAction(MovementCardData card, Vector2Int destination)
    {
        if (_actionsRemaining <= 0 || CurrentCharacter == null) return;

        // Обмеження за станами:
        // Critical: неможливість руху
        if (CurrentCharacter.CurrentState == HealthState.Critical)
        {
            Debug.LogWarning($"[{CurrentCharacter.Data.CharacterName}] is in Critical state and cannot move.");
            return;
        }

        // Come: персонаж може лише замінитись або пропустити хід (GDD),
        // рух у такому стані блокуємо.
        if (CurrentCharacter.CurrentState == HealthState.Come)
        {
            Debug.LogWarning($"[{CurrentCharacter.Data.CharacterName}] is in Come state and may only swap or skip the turn.");
            return;
        }

        // ������� MovementSystem �� ��������
        var movementSystem = CurrentCharacter.GetComponent<MovementSystem>();
        if (movementSystem == null)
        {
            Debug.LogError("MovementSystem component is missing on CurrentCharacter.");
            return;
        }

        // Якщо це друга дія руху підряд за хід — вважаємо це double-move (GDD)
        bool isDoubleMove = _hasMovedOnceThisTurn;

        movementSystem.MoveCharacter(
            destination,
            card,
            DeckManager,
            isDoubleMove
        );

        // Після double-move скидаємо прапорець, щоб не було triple-move
        _hasMovedOnceThisTurn = !isDoubleMove;
        _lastActionWasAttack = false;

        _actionsRemaining--;
        Debug.Log($"Action used: Move. Actions left: {_actionsRemaining}");

        CheckEndTurn();
    }

    // �������: �����
    public void PerformAttackAction(Tile targetTile)
    {
        if (_actionsRemaining <= 0 || CurrentCharacter == null) return;

        // GDD: заборона двох атак поспіль за один хід
        if (_lastActionWasAttack)
        {
            Debug.LogWarning($"[{CurrentCharacter.Data.CharacterName}] cannot perform two attacks in a row in a single turn.");
            return;
        }

        // Come: у стані коми персонаж не може атакувати (GDD)
        if (CurrentCharacter.CurrentState == HealthState.Come)
        {
            Debug.LogWarning($"[{CurrentCharacter.Data.CharacterName}] is in Come state and cannot attack. Use swap or skip instead.");
            return;
        }

        // Dead: додаткова перевірка безпеки
        if (CurrentCharacter.CurrentState == HealthState.Dead)
        {
            Debug.LogWarning($"[{CurrentCharacter.Data.CharacterName}] is Dead and cannot act.");
            return;
        }

        // Перевірка блокування атаки через риси (статус BlockAttack)
        if (TraitSystem.IsAttackBlocked(CurrentCharacter))
        {
            Debug.LogWarning($"[{CurrentCharacter.Data.CharacterName}] cannot attack: attack is blocked by trait.");
            return;
        }

        CombatManager.Instance.PerformAttack(CurrentCharacter, targetTile);

        _actionsRemaining--;
        Debug.Log($"Action used: Attack. Actions left: {_actionsRemaining}");

        // Після атаки фіксуємо, що остання дія була атакою
        _lastActionWasAttack = true;
        // Будь-яка атака розбиває ланцюжок double-move
        _hasMovedOnceThisTurn = false;

        CheckEndTurn();
    }

    // ����������� UI-������� ��� ���� ���������� ��
    public void EndCharacterTurnManually()
    {
        if (CurrentCharacter != null)
        {
            _actionsRemaining = 0;
            CheckEndTurn();
        }
    }

    private void CheckEndTurn()
    {
        if (_actionsRemaining <= 0)
        {
            Debug.Log($"P{PlayerID} turn with {CurrentCharacter.Data.CharacterName} ended.");
            CurrentCharacter = null;
            _gameManager.EndCharacterTurn();
        }
    }
}