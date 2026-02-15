using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthSystem : MonoBehaviour
{
    [SerializeField] private CharHealth _charHealth = CharHealth.NonInitialized;
    [SerializeField] private Image[] _healthBars = new Image[6];

    private enum CharHealth
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

    private void Start()
    {

    }
}