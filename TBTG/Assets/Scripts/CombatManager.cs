// CombatManager.cs
using UnityEngine;
using System.Linq;
using System.Collections.Generic; // ������ ��� ������������ Tile

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
    // ������� ����ֲ� �����
    // ----------------------------------------------------------------------
    /// <summary>
    /// ������ ������ ���� �����: ���������� ������������, ����� ������, ������� ����������.
    /// </summary>
    public void PerformAttack(Character attacker, Tile targetTile)
    {
        if (!targetTile.IsOccupied) return;

        Character target = targetTile.Occupant;

        // 1. ���������� ����Բ����в�
        int totalAdvantage = CalculateTotalAdvantage(attacker, target, targetTile);

        // 2. ����� ����ʲ� (3d6)
        int rollResult = Roll3D6();

        // 3. ����������ֲ� �� �����
        ProcessRollResult(rollResult, totalAdvantage, attacker, target);
    }

    /// <summary>
    /// ���� ����� 3 ������������ ������ (3d6).
    /// </summary>
    private int Roll3D6()
    {
        // �������� ���� ����� ���������� ����� �� 1 �� 6
        return Random.Range(1, 7) + Random.Range(1, 7) + Random.Range(1, 7);
    }

    // ----------------------------------------------------------------------
    // ��ò�� ����Բ����в� (��������/���������)
    // ----------------------------------------------------------------------
    /// <summary>
    /// �������� ��������� ����������� Advantage/Obstacle.
    /// </summary>
    /// <param name="attacker">��������, �� �����.</param>
    /// <param name="target">��������, �� ����� �������� �����.</param>
    /// <param name="targetTile">������� ���.</param>
    /// <returns>ֳ�� �����: -1 �� �������� (Advantage), +1 �� ��������� (Obstacle).</returns>
    private int CalculateTotalAdvantage(Character attacker, Character target, Tile targetTile)
    {
        int advantage = 0;

        // !!! �����������: ��������� ������� ������������� ����� GridManager !!!
        Tile attackerTile = GridManager.Instance.GetTile(attacker.GridPosition);
        if (attackerTile == null)
        {
            Debug.LogError("Attacker tile not found in GridManager! Returning 0 advantage.");
            return 0;
        }

        // 1. ����� ������ ��� (����� ��������� �� �������� �������������)
        if (target.CurrentState == HealthState.Light || target.CurrentState == HealthState.Heavy || target.CurrentState == HealthState.Critical)
        {
            advantage -= 1; // -1 �� Advantage
        }

        // 1.1. Штраф до атаки для пораненого/критичного атакуючого (GDD: Heavy/Critical)
        if (attacker.CurrentState == HealthState.Heavy || attacker.CurrentState == HealthState.Critical)
        {
            advantage += 1; // +1 �� Obstacle (ускладнюємо попадання)
        }

        // 2. ����� ������� (Attack-Side Advantage)

        // ������� ���� �������������
        if (attackerTile.Type == TileType.Offensive) advantage -= 1; // �������� ������� -> Advantage
        if (attackerTile.Type == TileType.Defensive) advantage += 1; // ������� ������� -> Obstacle

        // ������� ���� ���
        if (targetTile.Type == TileType.Defensive) advantage += 1;   // ������� ������� -> Obstacle
        if (targetTile.Type == TileType.Offensive) advantage -= 1;  // �������� ������� -> Advantage

        // ����� �������� �������
        if (attackerTile.Type == TileType.ActiveTile && attackerTile.CurrentEffect != null)
        {
            // AttackAdvantageChange: -1 ��� ��������, +1 ��� ���������
            advantage += attackerTile.CurrentEffect.AttackAdvantageChange;
        }

        // ... ������ �������� �� ���� �� ���� ������������ ...

        // GDD: сумарний модифікатор не може перевищувати ±3
        advantage = Mathf.Clamp(advantage, -3, 3);

        return advantage;
    }

    // ----------------------------------------------------------------------
    // ����������ֲ� ����������
    // ----------------------------------------------------------------------
    /// <summary>
    /// �������� ��������� ����� � ����������� �������/��������.
    /// </summary>
    private void ProcessRollResult(int roll, int advantage, Character attacker, Character target)
    {
        // �������� (-1) ������ ���� ��� �����, ��������� (+1) ������ ����.
        int effectiveRoll = roll + advantage;
        int impacts = 0;
        bool activateReactive = false;

        Debug.Log($"Roll: {roll}, Advantage Modifier: {advantage}, Effective Roll: {effectiveRoll}");

        // GDD 3d6:
        // 3  - критичний промах
        // 4–10 - промах
        // 11–14 - звичайне влучення (Impact 1, без реактивних рис)
        // 15–17 - посилене влучення (Impact 1 + Reactive traits)
        // 17–18 - крит (Impact 2 + посилені Reactive traits)

        if (effectiveRoll <= 3) // ��������� ������
        {
            Debug.Log("Critical Miss: Self-Impact 1");
            attacker.ApplyDamage(1);
        }
        else if (effectiveRoll >= 4 && effectiveRoll <= 10) // ������
        {
            Debug.Log("Miss.");
        }
        else if (effectiveRoll >= 11 && effectiveRoll <= 14) // �������� �������� (Impact 1)
        {
            Debug.Log("Standard Hit: Impact 1");
            impacts = 1;
        }
        else if (effectiveRoll >= 15 && effectiveRoll <= 17) // �������� �������� (Impact 1 + Traits)
        {
            Debug.Log("Reinforced Hit: Impact 1 + Reactive Traits");
            impacts = 1;
            activateReactive = true;
        }
        else if (effectiveRoll >= 18) // �������� �������� (Impact 2 + �������� Traits)
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
            // ��� ������� ����� ��������� Reactive Traits ��� (���������, ��������� ����� �� Character)
            // target.ActivateReactiveTraits();
        }
    }
}