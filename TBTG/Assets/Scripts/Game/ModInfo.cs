using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModInfo : MonoBehaviour
{
    public ModData ModData;

    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _price;
    [SerializeField] private Image _icon;

    private void Start()
    {
        _name.text = ModData.ModificatorName;
        _price.text = ModData.Price.ToString();
        _icon.sprite = ModData.Icon;
    }
}