using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthSystem : MonoBehaviour
{
    public static System.Action<int, int> OnHealthChanged;

    public int OwnerID { get; private set; } = -1;
    public int PairID { get; private set; } = -1;

    public CharHealth HealthState 
    { 
        get => _charHealth;
        set 
        {
            if (_charHealth != value)
            {
                _charHealth = value;
                OnHealthChanged?.Invoke(OwnerID, PairID);
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

    public void Initialize(int ownerID, int pairID)
    {
        OwnerID = ownerID;
        PairID = pairID;
    }

    public void TakeHit()
    {
        if (HealthState > CharHealth.Dead)
        {
            HealthState = (CharHealth)((int)HealthState - 1);
        }
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