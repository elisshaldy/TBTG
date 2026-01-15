using UnityEngine;
using System.Collections.Generic;

// [CreateAssetMenu] дозволяє створити цей ассет через меню Unity
[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Game Data/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Visuals")]
    public string CharacterName = "New Character";
    public Sprite CharacterSprite;

    // --- ДОДАНО: ОСНОВНІ ПОЛЯ ДЛЯ МЕНЕДЖЕРІВ ---
    [Header("Core Unit Stats")]

    [Tooltip("Максимальне здоров'я персонажа. Потрібно для ініціалізації юніта.")]
    public int MaxHealth = 100;

    [Tooltip("Максимальний діапазон руху за один хід.")]
    public int MovementRange = 3;
    // --- КІНЕЦЬ ДОДАНИХ ПОЛІВ ---


    [Header("Stats (Combat)")]
    public int AttackRollDice = 2;
    public int AttackRollSides = 6;
    public int BaseAttackBonus = 0;
    public int BaseDefenseBonus = 0;
    public int CriticalHealthLimit = 4;

    [Header("Attack Pattern")]
    // **НОВЕ ПОЛЕ:** Позиція клітинки персонажа (Black Tile) у сітці 3x3 (0,0 до 2,2)
    [Tooltip("Позиція персонажа (Black Tile) в сітці 3x3. Наприклад, (1, 1) - центр.")]
    public Vector2Int CenterTilePosition = new Vector2Int(1, 1);

    // Схема атаки: список зміщень відносно CenterTilePosition
    [Tooltip("Список зміщень (offset) від CenterTilePosition, які будуть червоними (Attack Tiles).")]
    public Vector2Int[] AttackPattern = new Vector2Int[]
    {
        // Приклад: Атака на одну клітинку вгору
        new Vector2Int(0, 1)
    };

    [Header("Resources")]
    [Tooltip("Максимальна кількість очок рис (UP) для цього персонажа")]
    public int MaxTraitPoints = 3;

    // Вважаємо, що у вас є клас TraitData. Якщо ні, вам потрібно буде його створити.
    [Tooltip("Список Traits, які персонаж набув.")]
    public List<TraitData> PurchasedTraits = new List<TraitData>();
    
    public AttackGrid3x3 AttackPatternGrid;
}

[System.Serializable]
public class AttackGrid3x3
{
    [HideInInspector] public Vector2Int CharacterPosition = new Vector2Int(1, 1);

    // y * 3 + x
    public bool[] Cells = new bool[9];

    public bool Get(int x, int y)
    {
        return Cells[y * 3 + x];
    }

    public void Set(int x, int y, bool value)
    {
        Cells[y * 3 + x] = value;
    }

    public bool IsCharacterTile(int x, int y)
    {
        return CharacterPosition.x == x && CharacterPosition.y == y;
    }
}