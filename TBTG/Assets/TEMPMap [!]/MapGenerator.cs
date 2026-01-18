using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public GameObject TilePrefab;
    public int TileSize = 1;
    
    [Header("Map Settings")]
    public int MapWidth = 12;
    public int MapHeight = 12;

    [Header("Fixed Tile Counts")]
    public int Count_Plain = 69;
    public int Count_Impassable = 25;
    public int Count_AttackableOnly = 19;
    public int Count_Offensive = 13;
    public int Count_Defensive = 13;
    public int Count_ActiveTile = 5; // Всього 3 активні клітинки

    public int minDistance = 3; // Мінімальна відстань між активними тайлами

    private GridManager _gridManager;
    private List<Vector2Int> _allCoordinates;

    void Start()
    {
        _gridManager = GridManager.Instance;
        if (_gridManager == null)
        {
            Debug.LogError("GridManager not found. Map generation aborted.");
            return;
        }

        // Встановлення розміру карти 9x9
        _gridManager.MapWidth = MapWidth;
        _gridManager.MapHeight = MapHeight;

        // Генерація
        GenerateFullMap();
    }

    private void GenerateFullMap()
    {
        // 1. Створення пулу всіх координат (0,0 до 8,8)
        _allCoordinates = new List<Vector2Int>();
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                _allCoordinates.Add(new Vector2Int(x, y));
            }
        }

        // 2. Створення фіксованого пулу типів клітинок (81 шт)
        List<TileType> tilePool = CreateFixedTilePool();

        // 3. Розміщення Active Tiles (центральний регіон)
        PlaceActiveTiles(tilePool);

        // 4. Розміщення решти клітинок, з перевіркою зв'язності для Impassable
        PlaceRemainingTiles(tilePool);

        // 5. Фінальна перевірка (якщо потрібно)
        // CheckConnectivity(); 

        Debug.Log("Map generation complete with fixed tile counts and central Active Tiles.");
    }

    // ----------------------------------------------------------------------
    // 1. Створення пулу фіксованої кількості клітинок
    // ----------------------------------------------------------------------
    private List<TileType> CreateFixedTilePool()
    {
        List<TileType> pool = new List<TileType>();

        AddTilesToPool(pool, TileType.Plain, Count_Plain);
        AddTilesToPool(pool, TileType.Impassable, Count_Impassable);
        AddTilesToPool(pool, TileType.AttackableOnly, Count_AttackableOnly);
        AddTilesToPool(pool, TileType.Offensive, Count_Offensive);
        AddTilesToPool(pool, TileType.Defensive, Count_Defensive);
        AddTilesToPool(pool, TileType.ActiveTile, Count_ActiveTile); // Активні поки в загальному пулі

        // Перевірка на всяк випадок
        if (pool.Count != 81)
        {
            Debug.LogError($"Tile pool count is incorrect: {pool.Count}. Expected 81.");
        }
        return pool;
    }

    private float CalculateProximityPenalty(Vector2Int coords, TileType type, Dictionary<Vector2Int, TileType> placedTiles)
    {
        float penalty = 0f;
        // Шукаємо вже розміщені клітинки того ж типу
        var sameTypeNeighbors = placedTiles.Where(kv => kv.Value == type);

        if (!sameTypeNeighbors.Any())
        {
            return 0f; // Штрафу немає, якщо це перша клітинка такого типу
        }

        // Для забезпечення максимальної віддаленості, ми хочемо МІНІМІЗУВАТИ цей штраф.
        // Штраф вищий, якщо клітинка ближче до існуючої.
        // Використовуємо 1 / Distance, щоб ближчі клітинки давали більший "штраф".

        foreach (var neighbor in sameTypeNeighbors)
        {
            float distance = Vector2Int.Distance(coords, neighbor.Key);

            // Додаємо інверсію відстані. Чим менша відстань (distance), тим більший штраф (penalty).
            // Додамо невелике зміщення (+0.1f), щоб уникнути ділення на нуль, якщо distance = 0.
            penalty += 1f / (distance + 0.1f);
        }

        return penalty;
    }


    // ***************************************************************
    // ОНОВЛЕНИЙ МЕТОД: Розміщення решти клітинок
    // ***************************************************************
    private void PlaceRemainingTiles(List<TileType> tilePool)
    {
        // Словник для відстеження всіх розміщених клітинок (для розрахунку штрафу)
        // Включає ActiveTiles, які вже були розміщені.
        Dictionary<Vector2Int, TileType> placedTiles = _gridManager.GetAllRegisteredCoords()
            .ToDictionary(coord => coord, coord => _gridManager.GetTile(coord).Type);

        // Перемішуємо пул типів, які залишилися, щоб не було пріоритету
        List<TileType> remainingTypes = tilePool.OrderBy(x => Random.value).ToList();

        foreach (TileType typeToPlace in remainingTypes)
        {
            // 1. Створюємо список "потенційних" координат та розраховуємо для них штраф
            // Використовуємо _allCoordinates, оскільки в ньому залишилися тільки вільні місця.

            Vector2Int bestCoord = Vector2Int.zero;
            float minPenalty = float.MaxValue;

            // Попередньо перемішуємо, щоб випадковий вибір був, якщо штрафи рівні
            List<Vector2Int> availableCoords = _allCoordinates.OrderBy(x => Random.value).ToList();

            foreach (Vector2Int potentialCoord in availableCoords)
            {
                float penalty = CalculateProximityPenalty(potentialCoord, typeToPlace, placedTiles);

                if (penalty < minPenalty)
                {
                    minPenalty = penalty;
                    bestCoord = potentialCoord;
                }
            }

            // 2. Логіка запобігання закритим просторам (залишається)
            TileType finalType = typeToPlace;
            if (typeToPlace == TileType.Impassable || typeToPlace == TileType.AttackableOnly)
            {
                if (!IsConnectivityMaintained(bestCoord, typeToPlace))
                {
                    finalType = TileType.Plain; // Замінюємо на Plain, якщо ізолює
                }
            }

            // 3. Розміщення та реєстрація
            CreateAndRegisterTile(bestCoord, finalType);

            // 4. Оновлення списків для наступної ітерації
            _allCoordinates.Remove(bestCoord); // Видаляємо з доступних
            placedTiles.Add(bestCoord, finalType); // Додаємо до розміщених для розрахунку штрафу
        }
    }

    private void AddTilesToPool(List<TileType> pool, TileType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            pool.Add(type);
        }
    }

    // ----------------------------------------------------------------------
    // 2. Розміщення Active Tiles (Центральний регіон)
    // ----------------------------------------------------------------------
    private void PlaceActiveTiles(List<TileType> tilePool)
    {
        // Центральний регіон 8x8 (від (2,2) до (9,9))
        List<Vector2Int> centralCoords = _allCoordinates.Where(
            c => c.x >= 2 && c.x <= MapWidth-3 && c.y >= 2 && c.y <= MapHeight- 3
        ).ToList();

        // Список для збереження розміщених активних тайлів
        List<Vector2Int> placedActiveTiles = new List<Vector2Int>();

        // Спеціальна функція для перевірки відстані
        bool IsFarEnough(Vector2Int coords, List<Vector2Int> placedTiles, int minDist)
        {
            foreach (var placed in placedTiles)
            {
                int dx = Math.Abs(coords.x - placed.x);
                int dy = Math.Abs(coords.y - placed.y);

                // Можна використовувати різні види відстані:
                // 1. Манхеттенська відстань (швидше обчислюється)
                if (dx + dy < minDist) return false;

                // 2. Чебишевська відстань (квадратна зона)
                // if (Math.Max(dx, dy) < minDist) return false;

                // 3. Євклідова відстань (діагональ відстань)
                // if (Math.Sqrt(dx*dx + dy*dy) < minDist) return false;
            }
            return true;
        }

        // Фільтруємо координати, щоб вони були на достатній відстані
        List<Vector2Int> availableCoords = new List<Vector2Int>();

        // Беремо випадкову координату для першого тайла
        centralCoords = centralCoords.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < Count_ActiveTile && centralCoords.Count > 0; i++)
        {
            Vector2Int selectedCoords = Vector2Int.zero;
            bool found = false;

            // Шукаємо координату, яка знаходиться на достатній відстані
            for (int j = 0; j < centralCoords.Count; j++)
            {
                if (IsFarEnough(centralCoords[j], placedActiveTiles, minDistance))
                {
                    selectedCoords = centralCoords[j];
                    found = true;

                    // Створення та реєстрація Active Tile
                    CreateAndRegisterTile(selectedCoords, TileType.ActiveTile);
                    placedActiveTiles.Add(selectedCoords);
                    tilePool.Remove(TileType.ActiveTile);
                    _allCoordinates.Remove(selectedCoords);

                    break;
                }
            }

            // Якщо не знайшли відповідну координату, перериваємо
            if (!found)
            {
                Debug.LogWarning($"Не вдалося знайти координату для активного тайла {i + 1} з мінімальною відстанню {minDistance}");
                break;
            }
        }
    }

    // ----------------------------------------------------------------------
    // 3. Розміщення решти клітинок з перевіркою
    // ----------------------------------------------------------------------

    private void CreateAndRegisterTile(Vector2Int coords, TileType type)
    {
        Vector3 worldPosition = new Vector3(coords.x * TileSize, 0, coords.y * TileSize);
        GameObject tileObj = Instantiate(TilePrefab, worldPosition, Quaternion.identity, this.transform);
        tileObj.name = $"Tile ({coords.x},{coords.y}) - {type}";

        Tile tile = tileObj.GetComponent<Tile>();
        tile.GridCoordinates = coords;
        tile.Type = type;

        // if (type == TileType.ActiveTile && ActiveTileEffectsPool.Any())
        // {
        //     tile.CurrentEffect = ActiveTileEffectsPool[Random.Range(0, ActiveTileEffectsPool.Count)];
        // }

        _gridManager.RegisterTile(coords, tile);
        ApplyVisuals(tileObj, type);
    }

    // ----------------------------------------------------------------------
    // 4. Перевірка Зв'язності (Connectivity Check)
    // ----------------------------------------------------------------------
    // Цей метод використовує алгоритм Flood Fill (або BFS/DFS)
    // щоб переконатися, що всі проходимі клітинки (ті, які не Impassable/AttackableOnly)
    // можуть бути досягнуті з однієї початкової точки.
    private bool IsConnectivityMaintained(Vector2Int potentialBlock, TileType newType)
    {
        // Створюємо тимчасову модель поля для перевірки
        Dictionary<Vector2Int, TileType> tempGrid = new Dictionary<Vector2Int, TileType>();

        // Заповнюємо тимчасову сітку вже створеними клітинками
        foreach (var coord in _gridManager.GetAllRegisteredCoords())
        {
            Tile tile = _gridManager.GetTile(coord);
            if (tile != null) tempGrid[coord] = tile.Type;
        }

        // Додаємо клітинки, які ми збираємося розмістити
        foreach (var coord in _allCoordinates)
        {
            // Якщо координати співпадають з поточною потенційною стіною
            if (coord == potentialBlock)
            {
                tempGrid[coord] = newType;
            }
            // Інакше ми поки що припускаємо, що це Plain (найбільш нейтральний тип)
            else if (!tempGrid.ContainsKey(coord))
            {
                tempGrid[coord] = TileType.Plain;
            }
        }

        // Вибираємо початкову точку для Flood Fill (наприклад, (0,0))
        Vector2Int startPoint = new Vector2Int(0, 0);
        if (tempGrid[startPoint] == TileType.Impassable || tempGrid[startPoint] == TileType.AttackableOnly)
            return true; // Якщо стартова точка вже заблокована (що малоймовірно), пропускаємо

        HashSet<Vector2Int> reachableTiles = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startPoint);
        reachableTiles.Add(startPoint);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Перевірка 4 напрямків
            Vector2Int[] neighbors = {
                current + new Vector2Int(1, 0), current + new Vector2Int(-1, 0),
                current + new Vector2Int(0, 1), current + new Vector2Int(0, -1)
            };

            foreach (var neighbor in neighbors)
            {
                if (neighbor.x >= 0 && neighbor.x < MapWidth && neighbor.y >= 0 && neighbor.y < MapHeight &&
                    !reachableTiles.Contains(neighbor) && tempGrid.ContainsKey(neighbor))
                {
                    TileType neighborType = tempGrid[neighbor];

                    // Умова прохідності: ми не повинні блокувати шлях
                    if (neighborType != TileType.Impassable && neighborType != TileType.AttackableOnly)
                    {
                        reachableTiles.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // Перевіряємо, чи всі 'прохідні' клітинки (не Impassable та не AttackableOnly) були досягнуті
        int totalWalkable = tempGrid.Count(
            kv => kv.Value != TileType.Impassable && kv.Value != TileType.AttackableOnly
        );

        // Якщо кількість досяжних клітинок менша за загальну кількість прохідних,
        // це означає, що ми ізолювали частину поля.
        return reachableTiles.Count == totalWalkable;
    }

    // ... (Метод ApplyVisuals залишається без змін) ...
    private void ApplyVisuals(GameObject tileObj, TileType type)
    {
        // Шукаємо компонент Renderer на об'єкті або його дочірніх елементах
        if (tileObj.TryGetComponent<Renderer>(out var renderer))
        {
            Color color;
            switch (type)
            {
                case TileType.Plain: color = Color.gray; break;         // Нейтральна
                case TileType.Impassable: color = Color.black; break;   // Непрохідна
                case TileType.Defensive: color = Color.green; break;    // Захисна (Бонус до захисту)
                case TileType.Offensive: color = Color.red; break;      // Атакуюча (Бонус до атаки)
                case TileType.ActiveTile: color = Color.yellow; break;  // Активна
                case TileType.AttackableOnly: color = Color.blue; break; // Тільки для атаки (Вода/Провалля)
                default: color = Color.white; break;
            }
            // Застосовуємо колір до матеріалу
            renderer.material.color = color;
        }
    }
}

public enum TileType
{
    Plain,
    Impassable,
    AttackableOnly,
    Defensive,
    Offensive,
    ActiveTile
}