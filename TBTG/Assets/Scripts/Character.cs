// Character.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    // Простий контейнер статусів, які накладаються рисами (наприклад, Curse).
    private HashSet<TraitStatusType> _activeStatuses = new HashSet<TraitStatusType>();

    public void Initialize(CharacterData data)
    {
        Data = data;
        // 1) Один і той самий TraitData не може бути присвоєний персонажу кілька разів.
        // На всяк випадок прибираємо дублікати за посиланням.
        _activeTraits = data.PurchasedTraits
            .Where(t => t != null)
            .Distinct()
            .ToList();
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
            
            // GDD: якщо один з пари загинув, вся пара йде у відбій
            // Сповіщаємо PairSystem про смерть персонажа
            PairSystem pairSystem = FindObjectOfType<PairSystem>();
            if (pairSystem != null)
            {
                pairSystem.HandleCharacterDeath(this);
            }
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

    /// <summary>
    /// Повертає активні риси персонажа (копія списку лише для читання).
    /// Використовується TraitSystem для оцінки ефектів.
    /// </summary>
    public IReadOnlyList<TraitData> GetActiveTraits()
    {
        return _activeTraits;
    }

    /// <summary>
    /// Перевірка, чи має персонаж певний статус (накладений рисами).
    /// </summary>
    public bool HasStatus(TraitStatusType status)
    {
        if (status == TraitStatusType.None) return false;
        return _activeStatuses.Contains(status);
    }

    /// <summary>
    /// Накласти статус на персонажа.
    /// (Поки що не використовується активно, але потрібний для traits, що працюють з прокляттями.)
    /// </summary>
    public void AddStatus(TraitStatusType status)
    {
        if (status == TraitStatusType.None) return;
        _activeStatuses.Add(status);
    }

    /// <summary>
    /// Зняти статус з персонажа.
    /// </summary>
    public void RemoveStatus(TraitStatusType status)
    {
        if (status == TraitStatusType.None) return;
        _activeStatuses.Remove(status);
    }

    // ... інші методи, пов'язані з персонажем
}