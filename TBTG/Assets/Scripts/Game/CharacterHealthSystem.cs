using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthSystem : MonoBehaviour
{
    public CharHealth HealthState => _charHealth;
    [SerializeField] private CharHealth _charHealth = CharHealth.Normal;

    public enum CharHealth
    {
        NonInitialized,
        Dead,
        Comma,
        Critical,
        Serious,
        Minor,  
        Normal,
        Extra
    }

    private void Awake()
    {
        if (_charHealth == CharHealth.NonInitialized)
            _charHealth = CharHealth.Normal;
    }
}