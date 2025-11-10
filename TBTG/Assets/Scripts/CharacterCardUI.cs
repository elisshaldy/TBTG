// CharacterCardUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Assertions;
using System.Collections.Generic;

// Додано для примусової перебудови UI-макета
using static UnityEngine.UI.LayoutRebuilder;

public class CharacterCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image CharacterImage; // Зображення персонажа
    public TextMeshProUGUI NameText; // Назва персонажа

    // Поля для відображення статистики
    public TextMeshProUGUI AttackBaseText;
    public TextMeshProUGUI DefenseBaseText;
    public TextMeshProUGUI HealthStateInfoText;

    // Посилання на батьківський об'єкт, що містить 9 UI-клітинок схеми атаки
    [Header("Attack Pattern Grid")]
    public Transform AttackGridContainer; // AttackGridContainer має бути RectTransform

    [Header("Grid Visuals Prefabs")]
    // Префаби для відображення клітинок
    public GameObject BlackTilePrefab;   // Позиція персонажа (чорний)
    public GameObject RedTilePrefab;     // Місце атаки (червоний)
    public GameObject EmptyTilePrefab;   // Порожня клітинка (білий/прозорий)

    private CharacterData _currentData;
    private const int GRID_SIZE = 3;

    private RectTransform _gridRectTransform;
    private GridLayoutGroup _gridLayoutGroup;

    void Awake()
    {
        // Отримуємо Layout Group один раз
        if (AttackGridContainer != null)
        {
            _gridLayoutGroup = AttackGridContainer.GetComponent<GridLayoutGroup>();
            if (_gridLayoutGroup == null)
            {
                Debug.LogWarning("GridLayoutGroup не знайдено на AttackGridContainer. Сітка не буде розміщена коректно.");
            }
        }
    }


    // Основний метод для оновлення картки
    public void DisplayCharacter(CharacterData data)
    {
        // Перевірка контейнера
        if (AttackGridContainer == null)
        {
            Debug.LogError("AttackGridContainer не призначено в Інспекторі!");
            return;
        }

        // Перевірка та ініціалізація RectTransform
        if (_gridRectTransform == null)
        {
            _gridRectTransform = AttackGridContainer.GetComponent<RectTransform>();
            Assert.IsNotNull(_gridRectTransform, "AttackGridContainer повинен мати компонент RectTransform!");
            if (_gridRectTransform == null) return;
        }

        // Додаткова перевірка Layout Group
        if (_gridLayoutGroup == null)
        {
            _gridLayoutGroup = AttackGridContainer.GetComponent<GridLayoutGroup>();
        }

        // Перевіряємо, чи має Layout Group коректний Cell Size
        if (_gridLayoutGroup != null)
        {
            Assert.IsTrue(_gridLayoutGroup.cellSize.x > 0 && _gridLayoutGroup.cellSize.y > 0,
                "Grid Layout Group на AttackGridContainer має Cell Size = (0, 0)! Сітка не відобразиться.");
        }

        _currentData = data;

        // Оновлення текстових полів
        NameText.text = data.CharacterName;

        string attackBonusString = (data.BaseAttackBonus > 0) ? $"+{data.BaseAttackBonus}" : "";
        AttackBaseText.text = $"Атака: {data.AttackRollDice}D{data.AttackRollSides} {attackBonusString}";

        DefenseBaseText.text = $"Захист: {(data.BaseDefenseBonus > 0 ? $"+{data.BaseDefenseBonus}" : "Базовий")}";

        HealthStateInfoText.text = $"Критичний Ліміт: {data.CriticalHealthLimit}";

        if (data.CharacterSprite != null)
        {
            CharacterImage.sprite = data.CharacterSprite;
        }

        // !!! Використовуємо CenterTilePosition з CharacterData !!!
        DrawAttackPattern(data.AttackPattern, data.CenterTilePosition);
    }

    private void DrawAttackPattern(Vector2Int[] pattern, Vector2Int center)
    {
        if (_gridRectTransform == null) return;

        // 1. Очищення попередньої схеми:
        int childCount = AttackGridContainer.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            // Використовуємо DestroyImmediate в режимі редактора або Destroy у режимі гри
            if (Application.isPlaying)
            {
                Destroy(AttackGridContainer.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(AttackGridContainer.GetChild(i).gameObject);
            }
        }

        // --- УНІФІКОВАНА ЛОГІКА СІТКИ ---

        // Створюємо список, що включає всі червоні клітинки та чорну клітинку
        List<Vector2Int> combinedPattern = new List<Vector2Int>(pattern);

        // Додаємо зміщення (0, 0) до шаблону атаки (це і є Black Tile)
        if (!combinedPattern.Contains(Vector2Int.zero))
        {
            combinedPattern.Add(Vector2Int.zero);
        }

        for (int y = 0; y < GRID_SIZE; y++)
        {
            for (int x = 0; x < GRID_SIZE; x++)
            {

                GameObject prefabToUse = EmptyTilePrefab;

                // 1. Інвертуємо Y-координату: 
                // Grid Layout Group малює зверху вниз (Y збільшується згори донизу), 
                // але наша логіка Game Data передбачає, що Y=0 - це нижня частина.
                Vector2Int gameCoord = new Vector2Int(x, GRID_SIZE - 1 - y);

                // 2. Розрахунок зміщення відносно позиції Black Tile (center)
                // Якщо CenterTilePosition в ассеті (0, 0), а gameCoord (0, 0), то relativeCoord = (0, 0)
                Vector2Int relativeCoord = gameCoord - center;

                if (combinedPattern.Contains(relativeCoord))
                {
                    // ВИЯВЛЕНО ПЕРСОНАЖА АБО АТАКУ
                    if (relativeCoord == Vector2Int.zero)
                    {
                        // Це клітинка персонажа (зміщення 0, 0)
                        prefabToUse = BlackTilePrefab;
                    }
                    else
                    {
                        // Це клітинка атаки (будь-яке інше зміщення)
                        prefabToUse = RedTilePrefab;
                    }
                }

                if (prefabToUse == null)
                {
                    Debug.LogError($"Tile Prefab is NULL! Check assignments in CharacterCardUI on {gameObject.name}.");
                    continue;
                }

                // Створення об'єкта та призначення батьківського елемента (вирішує проблему "Persistent Parent")
                GameObject tileUI = Instantiate(prefabToUse);
                tileUI.transform.SetParent(AttackGridContainer, false);

                // Активація
                if (!tileUI.activeSelf)
                {
                    tileUI.SetActive(true);
                }
            }
        }

        // ПРИМУСОВА ПЕРЕБУДОВА UI
        if (_gridRectTransform != null)
        {
            ForceRebuildLayoutImmediate(_gridRectTransform);
        }

        Debug.Log("Attack pattern tiles generated and rebuilt layout.");
    }
}