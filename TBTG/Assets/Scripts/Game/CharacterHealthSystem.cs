using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthSystem : MonoBehaviour
{
    public static System.Action OnAnyHealthChanged;

    public CharHealth HealthState 
    { 
        get => _charHealth;
        set 
        {
            if (_charHealth != value)
            {
                _charHealth = value;
                OnAnyHealthChanged?.Invoke();
            }
        }
    }
    [SerializeField] private CharHealth _charHealth = CharHealth.Normal;

    public enum CharHealth
    {
        NonInitialized = 0,
        Dead = 1,
        Coma = 2,
        Critical = 3,
        Serious = 4,
        Minor = 5,  
        Normal = 6,
        Extra = 7
    }

    public void SetHealth(CharHealth state)
    {
        HealthState = state;
    }

    private void Awake()
    {
        if (_charHealth == CharHealth.NonInitialized)
            _charHealth = CharHealth.Normal;
    }
}