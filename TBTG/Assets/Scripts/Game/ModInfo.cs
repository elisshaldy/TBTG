using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModInfo : MonoBehaviour
{
    public ModData ModData;

    [SerializeField] private TextMeshProUGUI _name; // LOCALE
    [SerializeField] private TextMeshProUGUI _price;
    [SerializeField] private Image _icon;
    
    public void Initialize()
    {
        _name.text = ModData.ModificatorName;
        _price.text = ModData.Price.ToString();
        _icon.sprite = ModData.Icon;
    }
}