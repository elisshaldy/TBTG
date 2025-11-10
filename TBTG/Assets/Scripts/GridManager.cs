// GridManager.cs
using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Map Settings")]
    public int MapWidth = 9;
    public int MapHeight = 9;

    // Словник для швидкого пошуку клітинки за координатами (X, Y)
    private Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();

    // Публічний метод для отримання всіх зареєстрованих координат
    public IEnumerable<Vector2Int> GetAllRegisteredCoords()
    {
        // Повертає колекцію ключів (координат) зі словника _tiles
        return _tiles.Keys;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Метод для реєстрації клітинок при старті гри/генерації поля
    public void RegisterTile(Vector2Int coords, Tile tile)
    {
        if (!_tiles.ContainsKey(coords))
        {
            _tiles.Add(coords, tile);
        }
    }

    // Публічний метод для CombatManager, щоб отримати клітинку за позицією
    public Tile GetTile(Vector2Int coords)
    {
        if (_tiles.TryGetValue(coords, out Tile tile))
        {
            return tile;
        }
        // Повертаємо null або кидаємо виняток, якщо клітинка не знайдена
        return null;
    }
}