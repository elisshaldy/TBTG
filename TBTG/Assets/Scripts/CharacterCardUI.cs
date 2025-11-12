// CharacterCardUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Assertions;
using System.Collections.Generic;

// Використовуємо статичний метод для примусової перебудови
using static UnityEngine.UI.LayoutRebuilder;

public class CharacterCardUI : MonoBehaviour
{
    // ... (Усі public поля залишаються без змін) ...
    [Header("UI References")]
    public Image CharacterImage;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI AttackBaseText;
    public TextMeshProUGUI DefenseBaseText;
    public TextMeshProUGUI HealthStateInfoText;

    [Header("Attack Pattern Grid")]
    public Transform AttackGridContainer;

    [Header("Grid Visuals Prefabs")]
    public GameObject BlackTilePrefab;
    public GameObject RedTilePrefab;
    public GameObject EmptyTilePrefab;

    private CharacterData _currentData; // Приватне поле для зберігання даних
    private const int GRID_SIZE = 3;

    private RectTransform _gridRectTransform;
    private GridLayoutGroup _gridLayoutGroup;

    void Awake()
    {
        if (AttackGridContainer != null)
        {
            // Отримання компонентів
            _gridLayoutGroup = AttackGridContainer.GetComponent<GridLayoutGroup>();
            _gridRectTransform = AttackGridContainer.GetComponent<RectTransform>();

            if (_gridLayoutGroup == null)
            {
                Debug.LogWarning("GridLayoutGroup не знайдено на AttackGridContainer. Сітка не буде розміщена коректно.");
            }
        }
    }

    // --- НОВИЙ МЕТОД: ВИПРАВЛЯЄ ПОМИЛКУ CS1061 ---
    /// <summary>
    /// Повертає ассет CharacterData, який наразі відображає ця картка.
    /// Потрібно для логіки виділення картки в PlayerCardManager.
    /// </summary>
    public CharacterData GetCurrentData()
    {
        return _currentData;
    }
    // ----------------------------------------------------

    // Основний метод для оновлення картки
    public void DisplayCharacter(CharacterData data)
    {
        if (data == null)
        {
            Debug.LogError("CharacterData is null on DisplayCharacter call!");
            return;
        }

        // Перевірка, чи ініціалізовані критичні RectTransform
        if (_gridRectTransform == null)
        {
            _gridRectTransform = AttackGridContainer.GetComponent<RectTransform>();
            if (_gridRectTransform == null) return;
        }


        _currentData = data; // Зберігаємо дані в приватному полі (це важливо для GetCurrentData)

        // Оновлення текстових полів з кращим форматуванням
        if (NameText != null) NameText.text = data.CharacterName;
        if (CharacterImage != null && data.CharacterSprite != null) CharacterImage.sprite = data.CharacterSprite;

        string attackBonusString = (data.BaseAttackBonus > 0) ? $"+{data.BaseAttackBonus}" : "";
        if (AttackBaseText != null) AttackBaseText.text = $"Атака: {data.AttackRollDice}D{data.AttackRollSides} {attackBonusString}";

        if (DefenseBaseText != null) DefenseBaseText.text = $"Захист: {(data.BaseDefenseBonus > 0 ? $"+{data.BaseDefenseBonus}" : "Базовий")}";

        if (HealthStateInfoText != null) HealthStateInfoText.text = $"Критичний Ліміт: {data.CriticalHealthLimit}";

        DrawAttackPattern(data.AttackPattern, data.CenterTilePosition);
    }

    private void DrawAttackPattern(Vector2Int[] pattern, Vector2Int center)
    {
        // ... (Логіка очищення та генерації плиток з другого коду залишається без змін) ...

        if (_gridRectTransform == null) return;

        // 1. Очищення попередньої схеми:
        for (int i = AttackGridContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(AttackGridContainer.GetChild(i).gameObject);
        }

        // Створюємо список, що включає всі червоні клітинки та чорну клітинку
        HashSet<Vector2Int> combinedPattern = new HashSet<Vector2Int>(pattern);

        // Додаємо зміщення (0, 0) до шаблону атаки (це і є Black Tile)
        combinedPattern.Add(Vector2Int.zero);


        for (int y = 0; y < GRID_SIZE; y++) // Ітеруємо згори донизу (UI-порядок)
        {
            for (int x = 0; x < GRID_SIZE; x++)
            {

                GameObject prefabToUse = EmptyTilePrefab;

                // 1. Інвертуємо Y-координату для порівняння з ігровими координатами (0,0 - низ)
                Vector2Int gameCoord = new Vector2Int(x, GRID_SIZE - 1 - y);

                // 2. Розрахунок зміщення відносно позиції Black Tile (center)
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

                // Створення об'єкта
                GameObject tileUI = Instantiate(prefabToUse);
                tileUI.transform.SetParent(AttackGridContainer, false);
            }
        }

        // ПРИМУСОВА ПЕРЕБУДОВА UI: КРАЩА ПРАКТИКА
        if (_gridRectTransform != null)
        {
            // Використовуємо MarkLayoutForRebuild замість ForceRebuildLayoutImmediate
            MarkLayoutForRebuild(_gridRectTransform);
        }
    }
}