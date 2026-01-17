using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Map Settings")]
    public int MapWidth = 9;
    public int MapHeight = 9;
    
    private Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();
    
    public IEnumerable<Vector2Int> GetAllRegisteredCoords()
    {
        // ������� �������� ������ (���������) � �������� _tiles
        return _tiles.Keys;
    }

    private void Awake()
    {
        // Singleton pattern: якщо вже є Instance, видаляємо поточний GameObject
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"GridManager instance already exists. Destroying duplicate: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        Debug.Log($"GridManager initialized on {gameObject.name}");
    }

    // ����� ��� ��������� ������� ��� ����� ���/��������� ����
    public void RegisterTile(Vector2Int coords, Tile tile)
    {
        if (!_tiles.ContainsKey(coords))
        {
            _tiles.Add(coords, tile);
        }
    }

    // �������� ����� ��� CombatManager, ��� �������� ������� �� ��������
    public Tile GetTile(Vector2Int coords)
    {
        if (_tiles.TryGetValue(coords, out Tile tile))
        {
            return tile;
        }
        // ��������� null ��� ������ �������, ���� ������� �� ��������
        return null;
    }
}