// PairSystem.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PairSystem : MonoBehaviour
{
    // Активний: резервний персонаж, Значення: зарезервований персонаж
    private Dictionary<CharacterData, CharacterData> _activeReservePairs = new Dictionary<CharacterData, CharacterData>();

    // Список всіх пар для зручності та відображення
    private List<CharacterPair> _allPairs = new List<CharacterPair>();

    [Tooltip("Посилання на GameManager для доступу до списків гравців та створення Character об'єктів.")]
    public GameManager GameManager; // Можна призначити в інспекторі!

    /// <summary>
    /// Ініціалізує систему пар, отримуючи список сформованих пар.
    /// </summary>
    public void Initialize(List<CharacterPair> formedPairs)
    {
        _activeReservePairs.Clear();
        _allPairs.Clear();

        foreach (var pair in formedPairs)
        {
            if (pair.ActiveCharacter != null)
            {
                _activeReservePairs.Add(pair.ActiveCharacter, pair.HiddenCharacter);
                _allPairs.Add(pair);
            }
        }

        Debug.Log($"PairSystem initialized with {formedPairs.Count} pairs.");
    }

    /// <summary>
    /// Перевіряє, чи можна замінити активного персонажа на резервного з пари.
    /// </summary>
    public bool CanSwap(CharacterData activeCharData)
    {
        return activeCharData != null &&
               _activeReservePairs.ContainsKey(activeCharData) &&
               _activeReservePairs[activeCharData] != null;
    }

    /// <summary>
    /// Виконує заміну активного персонажа на резервного з пари.
    /// </summary>
    public bool SwapPair(CharacterData activeCharData)
    {
        if (!CanSwap(activeCharData))
        {
            Debug.LogWarning($"Cannot swap {activeCharData?.CharacterName}: Pair not found or reserve is empty.");
            return false;
        }

        CharacterData hiddenCharData = _activeReservePairs[activeCharData];

        // 1. Оновлюємо словник пар
        _activeReservePairs.Remove(activeCharData);
        _activeReservePairs.Add(hiddenCharData, activeCharData);

        // 2. Оновлюємо CharacterPair об'єкт (для відображення)
        CharacterPair pairToUpdate = _allPairs.FirstOrDefault(p => p.ActiveCharacter == activeCharData);
        if (pairToUpdate != null)
        {
            pairToUpdate.ActiveCharacter = hiddenCharData;
            pairToUpdate.HiddenCharacter = activeCharData;
        }

        // 3. Знаходимо активного персонажа на полі
        if (GameManager == null)
        {
            GameManager = FindObjectOfType<GameManager>();
            if (GameManager == null)
            {
                Debug.LogError("GameManager not found! Cannot perform swap.");
                return false;
            }
        }

        Character currentActiveCharacter = GameManager.GetAllActiveCharacters()
            .FirstOrDefault(c => c != null && c.Data == activeCharData);

        if (currentActiveCharacter == null)
        {
            Debug.LogError($"Active Character object for {activeCharData.CharacterName} not found on the grid!");
            return false;
        }

        // Зберігаємо позицію та стан здоров'я (GDD: стани зберігаються при заміні)
        Vector2Int gridPos = currentActiveCharacter.GridPosition;
        HealthState savedState = currentActiveCharacter.CurrentState;
        Tile tile = GridManager.Instance.GetTile(gridPos);

        if (tile == null)
        {
            Debug.LogError($"Tile at {gridPos} not found!");
            return false;
        }

        // A. Деактивуємо поточного активного (переводимо в резерв)
        currentActiveCharacter.gameObject.SetActive(false);
        tile.RemoveOccupant();

        // B. Знаходимо або створюємо резервного персонажа
        Character newActiveCharacter = GameManager.GetInactiveCharacter(hiddenCharData);

        if (newActiveCharacter == null)
        {
            // Якщо резервний персонаж ще не створений, створюємо його
            // (це може статися, якщо персонаж ще не був розміщений на полі)
            Debug.LogWarning($"Inactive Character object for {hiddenCharData.CharacterName} not found. Creating new instance.");
            
            // Створюємо новий GameObject з компонентом Character
            GameObject newCharObj = new GameObject($"Character_{hiddenCharData.CharacterName}");
            newActiveCharacter = newCharObj.AddComponent<Character>();
            newActiveCharacter.Initialize(hiddenCharData);
        }

        // C. Активуємо нового активного персонажа
        newActiveCharacter.gameObject.SetActive(true);
        newActiveCharacter.transform.position = tile.transform.position;
        newActiveCharacter.GridPosition = gridPos;
        
        // Відновлюємо стан здоров'я (GDD: стани зберігаються при заміні в парі)
        // Якщо це перша заміна, резервний буде Unharmed, інакше - зберігаємо його попередній стан
        
        tile.SetOccupant(newActiveCharacter);

        // D. Оновлюємо списки в PlayerController
        int playerID = GameManager.GetPlayerID(currentActiveCharacter);
        if (playerID > 0)
        {
            GameManager.UpdatePlayerCharacterLists(playerID, currentActiveCharacter, newActiveCharacter);
        }
        else
        {
            Debug.LogWarning($"Could not determine PlayerID for character {activeCharData.CharacterName}");
        }

        // 4. Оновлення UI (опціонально) - інтегрується з UI-системою, яка відображає пари.

        Debug.Log($"Swap successful: {activeCharData.CharacterName} -> {hiddenCharData.CharacterName} at {gridPos}.");

        return true;
    }

    /// <summary>
    /// Обробляє смерть персонажа згідно з GDD: якщо один з пари загинув, вся пара йде у відбій.
    /// </summary>
    public void HandleCharacterDeath(Character deadCharacter)
    {
        if (deadCharacter == null || deadCharacter.Data == null) return;

        CharacterData deadCharData = deadCharacter.Data;
        
        // Перевіряємо, чи є цей персонаж у парах
        CharacterData pairMate = null;
        bool isActive = _activeReservePairs.ContainsKey(deadCharData);
        
        if (isActive)
        {
            // Це активний персонаж - знаходимо його резервного
            pairMate = _activeReservePairs[deadCharData];
        }
        else
        {
            // Це резервний персонаж - знаходимо його активного
            foreach (var kvp in _activeReservePairs)
            {
                if (kvp.Value == deadCharData)
                {
                    pairMate = kvp.Key;
                    break;
                }
            }
        }

        if (pairMate != null)
        {
            // GDD: вся пара йде у відбій
            Debug.Log($"Character {deadCharData.CharacterName} died. Removing entire pair (including {pairMate.CharacterName}) from the game.");
            
            // Видаляємо пару зі словника
            if (isActive)
            {
                _activeReservePairs.Remove(deadCharData);
            }
            else
            {
                _activeReservePairs.Remove(pairMate);
            }
            
            // Видаляємо пару зі списку
            _allPairs.RemoveAll(p => 
                (p.ActiveCharacter == deadCharData && p.HiddenCharacter == pairMate) ||
                (p.ActiveCharacter == pairMate && p.HiddenCharacter == deadCharData));
            
            // Деактивуємо партнера, якщо він активний на полі
            if (GameManager != null)
            {
                Character pairMateCharacter = GameManager.GetAllActiveCharacters()
                    .FirstOrDefault(c => c != null && c.Data == pairMate);
                
                if (pairMateCharacter != null)
                {
                    // Видаляємо з поля
                    Tile tile = GridManager.Instance.GetTile(pairMateCharacter.GridPosition);
                    if (tile != null)
                    {
                        tile.RemoveOccupant();
                    }
                    
                    // Деактивуємо GameObject
                    pairMateCharacter.gameObject.SetActive(false);
                    
                    // Видаляємо зі списку активних персонажів гравця
                    int playerID = GameManager.GetPlayerID(pairMateCharacter);
                    if (playerID > 0)
                    {
                        PlayerController player = (playerID == 1) ? GameManager.Player1 : GameManager.Player2;
                        if (player != null && player.ActiveCharacters != null)
                        {
                            player.ActiveCharacters.Remove(pairMateCharacter);
                        }
                    }
                }
            }
        }
        else
        {
            // Персонаж не в парі (мабуть, вже був видалений або це помилка)
            Debug.LogWarning($"Character {deadCharData.CharacterName} died, but no pair found to remove.");
        }
    }

    // Допоміжний метод для UI (наприклад, для відображення резервного персонажа)
    public CharacterData GetReserveCharacter(CharacterData activeCharData)
    {
        if (_activeReservePairs.TryGetValue(activeCharData, out var hidden))
        {
            return hidden;
        }
        return null;
    }
}
