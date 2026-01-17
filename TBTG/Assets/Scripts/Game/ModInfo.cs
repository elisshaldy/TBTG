using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModInfo : MonoBehaviour
{
    public ModData ModData;

    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _price;
    [SerializeField] private Image _icon;
    
    // TEMP
    [SerializeField] private Sprite[] tempIcons;

    private void Start()
    {
        _name.text = ModData.ModificatorName;
        _price.text = ModData.Price.ToString();
        _icon.sprite = ModData.Icon;
        
        // TEMP
        _icon.sprite = tempIcons[Random.Range(0, tempIcons.Length)];
    }
}