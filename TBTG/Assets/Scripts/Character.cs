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

    // Фактичний список рис, які можуть бути відкритими або прихованими
    private List<TraitData> _activeTraits = new List<TraitData>();
    private bool _isAttacker = false; // Використовується для логіки ініціативи

    public void Initialize(CharacterData data)
    {
        Data = data;
        _activeTraits = new List<TraitData>(data.PurchasedTraits);
        CurrentState = HealthState.Unharmed;
    }

    // Логіка трансформації стану
    public void ApplyDamage(int impacts)
    {
        // Це спрощений код. У реальності потрібна перевірка на Dead
        int newStateValue = (int)CurrentState - impacts;
        CurrentState = (HealthState)Mathf.Max(newStateValue, (int)HealthState.Dead);

        Debug.Log($"{Data.CharacterName} received {impacts} impact(s). New State: {CurrentState}");

        if (CurrentState == HealthState.Dead)
        {
            // Викликати подію загибелі
            gameObject.SetActive(false);
        }
    }

    // ... Методи руху, використання рис тощо
}