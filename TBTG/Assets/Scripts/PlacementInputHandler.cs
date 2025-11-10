// PlacementInputHandler.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlacementInputHandler : MonoBehaviour
{
    private GameManager _gameManager;
    private PlayerController _activePlayer;

    // Властивості для відстеження стану
    private Character _selectedCharacterForPlacement;
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

        Debug.Log($"Input Handler: P{_activePlayer.PlayerID}, оберіть персонажа зі списку CharactersToPlaceList.");
        _selectedCharacterForPlacement = null;

        // Тут у реальній грі ви підсвічуєте персонажів, доступних для розміщення.
    }

    public void StopListening()
    {
        _isPlacementPhaseActive = false;
        _selectedCharacterForPlacement = null;
        _activePlayer = null;
        Debug.Log("Input Handler: Очікування введення вимкнено.");
    }

    void Update()
    {
        if (!_isPlacementPhaseActive) return;

        if (Input.GetMouseButtonDown(0)) // Ліва кнопка миші
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        // Raycasting для визначення, на що клікнули
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 1. Спроба обрати персонажа
            if (_selectedCharacterForPlacement == null)
            {
                TrySelectCharacter(hit.transform.gameObject);
            }
            // 2. Спроба розмістити обраного персонажа на клітинці
            else
            {
                TryPlaceCharacter(hit.transform.gameObject);
            }
        }
    }

    private void TrySelectCharacter(GameObject clickedObject)
    {
        Character character = clickedObject.GetComponent<Character>();

        if (character != null && _activePlayer.CharactersToPlaceList.Contains(character))
        {
            _selectedCharacterForPlacement = character;
            Debug.Log($"ОБРАНО ПЕРСОНАЖА: {character.Data.CharacterName}. Тепер клікніть на доступну клітинку в зоні.");

            // Тут слід підсвітити доступну зону розміщення для кращого UX.
        }
    }

    private void TryPlaceCharacter(GameObject clickedObject)
    {
        Tile tile = clickedObject.GetComponent<Tile>();

        if (tile != null)
        {
            // Перевіряємо, чи клітинка в дозволеній зоні
            List<Vector2Int> allowedZone = _gameManager.GetPlayerPlacementZone(_activePlayer.PlayerID);

            if (allowedZone.Contains(tile.GridCoordinates))
            {
                // Викликаємо PlayerController для виконання розміщення
                _activePlayer.PlaceCharacter(_selectedCharacterForPlacement, tile.GridCoordinates);

                // Якщо розміщення успішне, InputHandler буде зупинений
                // (це робиться через StartNextPlacementTurn -> StopListening)
            }
            else
            {
                Debug.LogWarning("Клітинка знаходиться поза дозволеною зоною розміщення!");
            }
        }
    }
}