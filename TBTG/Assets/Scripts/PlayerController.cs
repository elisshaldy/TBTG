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
    private List<Vector2Int> _currentPlacementZone; // Зона, встановлена GameManager

    public void Initialize(int id, GameManager manager)
    {
        PlayerID = id;
        _gameManager = manager;

        _charactersToPlaceQueue = new Queue<Character>(CharactersToPlaceList);

        if (DeckManager == null) DeckManager = gameObject.AddComponent<MovementDeckManager>();
        DeckManager.DrawInitialHand();
    }

    // ----------------------------------------------------------------------
    // !!! ВИПРАВЛЕННЯ CS1061: ДОДАНО МЕТОД !!!
    // ----------------------------------------------------------------------
    /// <summary>
    /// Встановлює дозволену зону розміщення для гравця.
    /// Викликається GameManager на початку Placement Phase.
    /// </summary>
    public void SetPlacementZone(List<Vector2Int> zone)
    {
        _currentPlacementZone = zone;
        Debug.Log($"Player {PlayerID} placement zone set. Zone size: {zone.Count}");
    }
    // ----------------------------------------------------------------------
    // КІНЕЦЬ ВИПРАВЛЕННЯ
    // ----------------------------------------------------------------------

    // ----------------------------------------------------------------------
    // ФАЗА 1.1: Розміщення персонажа
    // ----------------------------------------------------------------------
    /// <summary>
    /// Починає хід розміщення для цього гравця.
    /// </summary>
    public void StartPlacement() // Оновлено: прибрано List<Vector2Int> availableZone з аргументів
    {
        if (!_charactersToPlaceQueue.Any())
        {
            Debug.Log($"Player {PlayerID} finished placement. Calling EndPlacementPhase.");
            // Тут має бути виклик методу на GameManager для переходу до наступного гравця
            // Або для виклику EndPlacementPhase
            return;
        }

        Character characterToPlace = _charactersToPlaceQueue.Peek(); // Просто беремо наступного, але не видаляємо

        Debug.Log($"Player {PlayerID} must place {characterToPlace.Data.CharacterName}.");

        // Активуємо InputHandler, передаємо йому PlayerController
        // InputHandler повинен знати, що йому тепер потрібно перевіряти _currentPlacementZone
        // Примітка: Ви можете додати _currentPlacementZone в StartListening, якщо хочете
        InputHandler.StartListening(this);
    }

    /// <summary>
    /// Фактичне розміщення персонажа на клітинці. Викликається InputHandler.
    /// </summary>
    public void PlaceCharacter(Character character, Vector2Int coords)
    {
        if (_charactersToPlaceQueue.Peek() != character)
        {
            Debug.LogWarning("Спроба розмістити не того персонажа. Видаліть це в фінальній версії.");
            return;
        }

        _charactersToPlaceQueue.Dequeue(); // Видаляємо з черги

        // Фізичне розміщення на полі
        // 1. Оновлення позиції Character
        character.GridPosition = coords;

        // 2. Оновлення клітинки (Tile)
        Tile tile = _gameManager.GridManager.GetTile(coords);
        if (tile != null)
        {
            tile.SetOccupant(character);
            character.transform.position = tile.transform.position; // Переміщення об'єкта
        }

        ActiveCharacters.Add(character);
        Debug.Log($"P{PlayerID} placed {character.Data.CharacterName} at {coords}.");

        InputHandler.StopListening();

        // Переходимо до розміщення наступного персонажа
        // _gameManager.StartNextPlacementTurn(); // Це має бути викликано GameManager
    }

    // ----------------------------------------------------------------------
    // ФАЗА 3: Виконання Ходу
    // ----------------------------------------------------------------------

    public void StartTurn(Character character)
    {
        CurrentCharacter = character;
        _actionsRemaining = 2; // Кожному персонажу 2 дії

        // Тут має бути логіка підсвічування цього персонажа, оновлення UI тощо.

        Debug.Log($"P{PlayerID} turn started with {CurrentCharacter.Data.CharacterName}. Actions left: {_actionsRemaining}");
    }

    // Приклад: Рух персонажа
    public void PerformMoveAction(MovementCardData card, Vector2Int destination)
    {
        if (_actionsRemaining <= 0 || CurrentCharacter == null) return;

        // Потрібен MovementSystem на персонажі
        // CurrentCharacter.GetComponent<MovementSystem>().MoveCharacter(
        //     destination, card, DeckManager, false
        // );

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
            Debug.Log($"P{PlayerID} turn with {CurrentCharacter.Data.CharacterName} ended.");
            CurrentCharacter = null;
            _gameManager.EndCharacterTurn();
        }
    }
}