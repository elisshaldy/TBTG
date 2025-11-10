// CombatManager.cs
using UnityEngine;
using System.Linq;

public class CombatManager : MonoBehaviour
{
    // ... (Частина Singleton та Awake залишається без змін) ...
    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ----------------------------------------------------------------------
    // ОСНОВНА ФУНКЦІЯ АТАКИ
    // ----------------------------------------------------------------------
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

    private int Roll3D6()
    {
        // ... (Логіка 3d6 залишається без змін) ...
        return Random.Range(1, 7) + Random.Range(1, 7) + Random.Range(1, 7); // 3d6
    }

    // ----------------------------------------------------------------------
    // ЛОГІКА МОДИФІКАТОРІВ (Змінено рядок отримання attackerTile)
    // ----------------------------------------------------------------------
    private int CalculateTotalAdvantage(Character attacker, Character target, Tile targetTile)
    {
        int advantage = 0; // -1 це перевага (Advantage), +1 це перешкода (Obstacle)

        // !!! ВИПРАВЛЕННЯ: Отримання клітинки атакувальника через GridManager !!!
        Tile attackerTile = GridManager.Instance.GetTile(attacker.GridPosition);
        if (attackerTile == null)
        {
            Debug.LogError("Attacker tile not found in GridManager!");
            return 0;
        }

        // 1. Вплив станів цілі
        if (target.CurrentState == HealthState.Light || target.CurrentState == HealthState.Heavy || target.CurrentState == HealthState.Critical)
        {
            advantage -= 1;
        }

        // 2. Вплив клітинок (Attack-Side Advantage)

        // Клітинки поля
        if (attackerTile.Type == TileType.Offensive) advantage -= 1;
        if (attackerTile.Type == TileType.Defensive) advantage += 1;
        if (targetTile.Type == TileType.Defensive) advantage += 1;
        if (targetTile.Type == TileType.Offensive) advantage -= 1;

        // Вплив активних клітинок
        if (attackerTile.Type == TileType.ActiveTile && attackerTile.CurrentEffect != null)
        {
            advantage += attackerTile.CurrentEffect.AttackAdvantageChange;
        }

        // ... Додати перевірку на риси та інші модифікатори

        return advantage;
    }

    // ... (Логіка ProcessRollResult залишається без змін) ...
    private void ProcessRollResult(int roll, int advantage, Character attacker, Character target)
    {
        int effectiveRoll = roll - advantage; // Перевага зменшує ефективний поріг
        int impacts = 0;
        bool activateReactive = false;

        Debug.Log($"Roll: {roll}, Advantage: {advantage}, Effective Roll: {effectiveRoll}");

        if (effectiveRoll <= 3) // Критичний промах
        {
            Debug.Log("Critical Miss: Self-Impact 1");
            attacker.ApplyDamage(1);
        }
        else if (effectiveRoll >= 4 && effectiveRoll <= 8) // Промах
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
            // Тут потрібна логіка активації Reactive Traits
        }
    }
}