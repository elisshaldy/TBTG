// CombatManager.cs
using UnityEngine;
using System.Linq;
using System.Collections.Generic; // Додано для використання Tile

public class CombatManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ----------------------------------------------------------------------
    // ОСНОВНА ФУНКЦІЯ АТАКИ
    // ----------------------------------------------------------------------
    /// <summary>
    /// Виконує повний цикл атаки: розрахунок модифікаторів, кидок кубиків, обробка результату.
    /// </summary>
    public void PerformAttack(Character attacker, Tile targetTile)
    {
        if (!targetTile.IsOccupied) return;

        Character target = targetTile.Occupant;

        // 1. РОЗРАХУНОК МОДИФІКАТОРІВ
        int totalAdvantage = CalculateTotalAdvantage(attacker, target, targetTile);

        // 2. КИДОК КУБИКІВ (3d6)
        int rollResult = Roll3D6();

        // 3. ІНТЕРПРЕТАЦІЯ ТА ВПЛИВ
        ProcessRollResult(rollResult, totalAdvantage, attacker, target);
    }

    /// <summary>
    /// Імітує кидок 3 шестигранних кубиків (3d6).
    /// </summary>
    private int Roll3D6()
    {
        // Обчислює суму трьох випадкових чисел від 1 до 6
        return Random.Range(1, 7) + Random.Range(1, 7) + Random.Range(1, 7);
    }

    // ----------------------------------------------------------------------
    // ЛОГІКА МОДИФІКАТОРІВ (Перевага/Перешкода)
    // ----------------------------------------------------------------------
    /// <summary>
    /// Обчислює загальний модифікатор Advantage/Obstacle.
    /// </summary>
    /// <param name="attacker">Персонаж, що атакує.</param>
    /// <param name="target">Персонаж, на якого націлена атака.</param>
    /// <param name="targetTile">Клітинка цілі.</param>
    /// <returns>Ціле число: -1 це Перевага (Advantage), +1 це Перешкода (Obstacle).</returns>
    private int CalculateTotalAdvantage(Character attacker, Character target, Tile targetTile)
    {
        int advantage = 0;

        // !!! ВИПРАВЛЕННЯ: Отримання клітинки атакувальника через GridManager !!!
        Tile attackerTile = GridManager.Instance.GetTile(attacker.GridPosition);
        if (attackerTile == null)
        {
            Debug.LogError("Attacker tile not found in GridManager! Returning 0 advantage.");
            return 0;
        }

        // 1. Вплив станів цілі (легке поранення дає Перевагу атакувальнику)
        if (target.CurrentState == HealthState.Light || target.CurrentState == HealthState.Heavy || target.CurrentState == HealthState.Critical)
        {
            advantage -= 1; // -1 це Advantage
        }

        // 2. Вплив клітинок (Attack-Side Advantage)

        // Клітинки поля атакувальника
        if (attackerTile.Type == TileType.Offensive) advantage -= 1; // Атакуюча клітинка -> Advantage
        if (attackerTile.Type == TileType.Defensive) advantage += 1; // Захисна клітинка -> Obstacle

        // Клітинки поля цілі
        if (targetTile.Type == TileType.Defensive) advantage += 1;   // Захисна клітинка -> Obstacle
        if (targetTile.Type == TileType.Offensive) advantage -= 1;  // Атакуюча клітинка -> Advantage

        // Вплив активних клітинок
        if (attackerTile.Type == TileType.ActiveTile && attackerTile.CurrentEffect != null)
        {
            // AttackAdvantageChange: -1 для переваги, +1 для перешкоди
            advantage += attackerTile.CurrentEffect.AttackAdvantageChange;
        }

        // ... Додати перевірку на риси та інші модифікатори ...

        return advantage;
    }

    // ----------------------------------------------------------------------
    // ІНТЕРПРЕТАЦІЯ РЕЗУЛЬТАТУ
    // ----------------------------------------------------------------------
    /// <summary>
    /// Обробляє результат кидка з урахуванням переваг/перешкод.
    /// </summary>
    private void ProcessRollResult(int roll, int advantage, Character attacker, Character target)
    {
        // Перевага (-1) зменшує поріг для успіху, Перешкода (+1) збільшує його.
        int effectiveRoll = roll + advantage;
        int impacts = 0;
        bool activateReactive = false;

        Debug.Log($"Roll: {roll}, Advantage Modifier: {advantage}, Effective Roll: {effectiveRoll}");

        if (effectiveRoll <= 3) // Критичний промах (Original Roll + Advantage <= 3)
        {
            Debug.Log("Critical Miss: Self-Impact 1");
            attacker.ApplyDamage(1);
        }
        else if (effectiveRoll >= 4 && effectiveRoll <= 8) // Промах (Original Roll + Advantage: 4-8)
        {
            Debug.Log("Miss.");
        }
        else if (effectiveRoll >= 9 && effectiveRoll <= 12) // Звичайне влучення (Impact 1)
        {
            Debug.Log("Standard Hit: Impact 1");
            impacts = 1;
        }
        else if (effectiveRoll >= 13 && effectiveRoll <= 16) // Посилене влучення (Impact 1 + Traits)
        {
            Debug.Log("Reinforced Hit: Impact 1 + Reactive Traits");
            impacts = 1;
            activateReactive = true;
        }
        else if (effectiveRoll >= 17) // Критичне влучення (Impact 2 + Посилені Traits)
        {
            Debug.Log("Critical Hit: Impact 2 + Enhanced Reactive Traits");
            impacts = 2;
            activateReactive = true;
        }

        if (impacts > 0)
        {
            target.ApplyDamage(impacts);
        }

        if (activateReactive)
        {
            // Тут потрібна логіка активації Reactive Traits цілі (наприклад, викликати метод на Character)
            // target.ActivateReactiveTraits();
        }
    }
}