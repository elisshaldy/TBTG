using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardInfo : MonoBehaviour
{
    public CharacterData CharData;

    [SerializeField] private TextMeshProUGUI _charName; // LOCALE
    [SerializeField] private Image _charImage;
    
    [SerializeField] private TextMeshProUGUI _attackBaseTxt; // LOCALE
    [SerializeField] private TextMeshProUGUI _defenseBaseTxt; // LOCALE

    // attack grid too

    public void Initialize()
    {
        _charName.text = CharData.CharacterName;
        _charImage.sprite = CharData.CharacterSprite;
    }
}