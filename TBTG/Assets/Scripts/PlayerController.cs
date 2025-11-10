// PlayerController.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    [Header("Configuration")]
    public int PlayerID;
    public PlacementInputHandler InputHandler; // Призначити в Інспекторі

    [Header("Placement State")]
    // Заповнити цей список в Інспекторі на початку гри
    public List<Character> CharactersToPlaceList;
    private Queue<Character> _charactersToPlaceQueue;

    public List<Character> ActiveCharacters = new List<Character>();

    [Header("Dependencies")]
    public MovementDeckManager DeckManager;

    [Header("Turn State")]
    public Character CurrentCharacter;
    private int _actionsRemaining = 2;
    private GameManager _gameManager;
    private List<Vector2Int> _currentPlacementZone;

    public void Initialize(int id, GameManager manager)
    {
        PlayerID = id;
        _gameManager = manager;

        _charactersToPlaceQueue = new Queue<Character>(CharactersToPlaceList);

        if (DeckManager == null) DeckManager = gameObject.AddComponent<MovementDeckManager>();
        DeckManager.DrawInitialHand();
    }

    // ----------------------------------------------------------------------
    // ФАЗА 1.1: Розміщення персонажа
    // ----------------------------------------------------------------------
    public void StartPlacement(List<Vector2Int> availableZone)
    {
        if (!_charactersToPlaceQueue.Any())
        {
            _gameManager.CompletePlacementTurn();
            return;
        }

        _currentPlacementZone = availableZone;
        Character charToPlace = _charactersToPlaceQueue.Peek();

        Debug.Log($"P{PlayerID} must place {charToPlace.Data.CharacterName} in zone: {string.Join(", ", availableZone)}");

        // Передаємо керування InputHandler
        if (InputHandler != null)
        {
            InputHandler.StartListening(this);
        }
    }

    // Перевірка умов розміщення: тип клітинки та її зайнятість
    private bool IsValidPlacement(Vector2Int coord)
    {
        Tile tile = _gameManager.GridManager.GetTile(coord);
        if (tile == null) return false;

        // Умова 1: НЕ Impassable або AttackableOnly
        if (tile.Type == TileType.Impassable || tile.Type == TileType.AttackableOnly)
            return false;

        // Умова 2: Клітинка має бути вільною
        if (tile.IsOccupied)
            return false;

        return true;
    }

    // Викликається InputHandler після вибору Tile
    public void PlaceCharacter(Character charToPlace, Vector2Int coord)
    {
        // Фінальна перевірка валідності (тепер включає перевірку, чи charToPlace є в списку на розміщення)
        if (!_currentPlacementZone.Contains(coord) || !IsValidPlacement(coord) || !CharactersToPlaceList.Contains(charToPlace))
        {
            Debug.LogWarning("Invalid placement attempt or character not in list.");
            return;
        }

        // Зупиняємо прослуховування вводу, оскільки розміщення успішне
        InputHandler.StopListening();

        // Видаляємо персонажа зі списку для розміщення
        CharactersToPlaceList.Remove(charToPlace);

        // ... (Логіка розміщення, як була раніше) ...
        Tile targetTile = _gameManager.GridManager.GetTile(coord);

        charToPlace.transform.position = targetTile.transform.position;
        targetTile.SetOccupant(charToPlace);
        charToPlace.GridPosition = coord;

        ActiveCharacters.Add(charToPlace);

        Debug.Log($"P{PlayerID} placed {charToPlace.Data.CharacterName} at {coord}.");

        // Завершення ходу розміщення
        _gameManager.CompletePlacementTurn();
    }

    // ----------------------------------------------------------------------
    // ФАЗА 3: Виконання Ходу (Бойова)
    // ----------------------------------------------------------------------
    public void StartTurn(Character character)
    {
        CurrentCharacter = character;
        _actionsRemaining = 2;

        Debug.Log($"P{PlayerID} turn started with {CurrentCharacter.Data.CharacterName}. Actions left: {_actionsRemaining}");
    }

    // Приклад: Рух персонажа
    public void PerformMoveAction(MovementCardData card, Vector2Int destination)
    {
        if (_actionsRemaining <= 0 || CurrentCharacter == null) return;

        CurrentCharacter.GetComponent<MovementSystem>().MoveCharacter(
            destination, card, DeckManager, false
        );

        _actionsRemaining--;
        Debug.Log($"Action used: Move. Actions left: {_actionsRemaining}");

        CheckEndTurn();
    }

    // Приклад: Атака
    public void PerformAttackAction(Tile targetTile)
    {
        if (_actionsRemaining <= 0 || CurrentCharacter == null) return;

        CombatManager.Instance.PerformAttack(CurrentCharacter, targetTile);

        _actionsRemaining--;
        Debug.Log($"Action used: Attack. Actions left: {_actionsRemaining}");

        CheckEndTurn();
    }

    // Викликається UI-кнопкою або після вичерпання дій
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
            Debug.Log($"P{PlayerID} turn with {CurrentCharacter.Data.CharacterName} finished.");
            CurrentCharacter = null;

            _gameManager.EndCharacterTurn();
        }
    }
}