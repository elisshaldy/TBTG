// PlacementInputHandler.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlacementInputHandler : MonoBehaviour
{
    private GameManager _gameManager;
    private PlayerController _activePlayer;

    // Стан для drag-and-drop розміщення
    private PairCardDragHandler _currentDraggedPair = null;
    private CharacterPair _currentPairForPlacement = null;
    private bool _isPlacementPhaseActive = false;

    void Start()
    {
        _gameManager = GameManager.Instance;
        if (_gameManager == null) Debug.LogError("GameManager instance not found.");
    }

    public void StartListening(PlayerController player)
    {
        _activePlayer = player;
        _isPlacementPhaseActive = true;

        Debug.Log($"Input Handler: P{_activePlayer.PlayerID}, ready for pair drag-and-drop placement.");
        _currentPairForPlacement = null;
        _currentDraggedPair = null;
    }

    public void StopListening()
    {
        _isPlacementPhaseActive = false;
        _currentPairForPlacement = null;
        _currentDraggedPair = null;
        _activePlayer = null;
        Debug.Log("Input Handler: Placement phase stopped.");
    }

    /// <summary>
    /// Викликається PairCardDragHandler при початку перетягування пари.
    /// </summary>
    public void OnPairDragStarted(PairCardDragHandler dragHandler, CharacterPair pair)
    {
        if (!_isPlacementPhaseActive) return;

        _currentDraggedPair = dragHandler;
        _currentPairForPlacement = pair;

        Debug.Log($"PlacementInputHandler: Started dragging pair {pair.ActiveCharacter.CharacterName}");
    }

    /// <summary>
    /// Викликається PairCardDragHandler при завершенні перетягування пари.
    /// </summary>
    public void OnPairDragEnded(PairCardDragHandler dragHandler, bool wasPlaced)
    {
        if (_currentDraggedPair == dragHandler)
        {
            _currentDraggedPair = null;
            _currentPairForPlacement = null;
        }

        Debug.Log($"PlacementInputHandler: Ended dragging pair, placed: {wasPlaced}");
    }

    /// <summary>
    /// Перевіряє, чи клітинка валідна для розміщення.
    /// </summary>
    public bool IsValidPlacementTile(Tile tile)
    {
        if (!_isPlacementPhaseActive || _activePlayer == null || tile == null)
        {
            return false;
        }

        // Перевірка типу клітинки
        if (tile.Type == TileType.Impassable || tile.Type == TileType.AttackableOnly)
        {
            return false;
        }

        // Перевірка, чи клітинка вже зайнята
        if (tile.IsOccupied)
        {
            return false;
        }

        // Перевірка, чи клітинка в зоні розміщення гравця
        List<Vector2Int> allowedZone = _gameManager.GetPlayerPlacementZone(_activePlayer.PlayerID);
        if (!allowedZone.Contains(tile.GridCoordinates))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Викликається PairCardDragHandler при успішному drop на клітинку.
    /// </summary>
    public void HandlePairDroppedOnTile(PairCardDragHandler dragHandler, Vector2Int tileCoords)
    {
        if (!_isPlacementPhaseActive || _activePlayer == null || _currentPairForPlacement == null)
        {
            Debug.LogWarning("PlacementInputHandler: Cannot place pair - invalid state.");
            return;
        }

        // Знаходимо активного персонажа з пари в списку для розміщення
        Character characterToPlace = _activePlayer.CharactersToPlaceList
            .FirstOrDefault(c => c != null && c.Data == _currentPairForPlacement.ActiveCharacter);

        if (characterToPlace == null)
        {
            Debug.LogWarning($"PlacementInputHandler: Character {_currentPairForPlacement.ActiveCharacter.CharacterName} not found in CharactersToPlaceList.");
            return;
        }

        // Розміщуємо персонажа
        _activePlayer.PlaceCharacter(characterToPlace, tileCoords);

        // Очищаємо стан
        _currentPairForPlacement = null;
        _currentDraggedPair = null;
    }
}
