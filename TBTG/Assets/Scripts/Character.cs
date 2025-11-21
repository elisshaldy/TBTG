// Character.cs
using UnityEngine;
using System.Collections.Generic;

public class Character : MonoBehaviour
{
    [Header("Data")]
    public CharacterData Data;

    [Header("Current State")]
    public HealthState CurrentState = HealthState.Unharmed;
    public Vector2Int GridPosition;

    // ��������� ������ ���, �� ������ ���� ��������� ��� �����������
    private List<TraitData> _activeTraits = new List<TraitData>();
    private bool _isAttacker = false; // ��������������� ��� ����� ����������

    public void Initialize(CharacterData data)
    {
        Data = data;
        _activeTraits = new List<TraitData>(data.PurchasedTraits);
        CurrentState = HealthState.Unharmed;
    }

    // ����� ������������� �����
    public void ApplyDamage(int impacts)
    {
        if (impacts <= 0) return;

        // �� ��������� ���. � ��������� ������� �������� �� Dead
        int newStateValue = (int)CurrentState - impacts;
        CurrentState = (HealthState)Mathf.Max(newStateValue, (int)HealthState.Dead);

        Debug.Log($"{Data.CharacterName} received {impacts} impact(s). New State: {CurrentState}");

        if (CurrentState == HealthState.Dead)
        {
            // ��������� ���� �������
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Лікування персонажа на задану кількість impact-ів.
    /// Відповідає правилу підняття стану (1 impact = +1 рівень).
    /// </summary>
    /// <param name="impacts">Скільки "сходинок" вгору підняти стан.</param>
    public void ApplyHealing(int impacts)
    {
        if (impacts <= 0) return;

        // За GDD персонажі у стані Death видаляються з поля, тож не лікуємо мертвих
        if (CurrentState == HealthState.Dead)
        {
            Debug.LogWarning($"{Data.CharacterName} is Dead and cannot be healed.");
            return;
        }

        int newStateValue = (int)CurrentState + impacts;

        // За замовчуванням обмежуємо лікування станом Unharmed,
        // Extra лишаємо для спеціальних ефектів (traits тощо).
        newStateValue = Mathf.Min(newStateValue, (int)HealthState.Unharmed);

        CurrentState = (HealthState)newStateValue;
        Debug.Log($"{Data.CharacterName} healed by {impacts} impact(s). New State: {CurrentState}");
    }

    // ... ������ ����, ������������ ��� ����
}