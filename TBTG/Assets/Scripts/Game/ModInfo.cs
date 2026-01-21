using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModInfo : MonoBehaviour
{
    public ModData ModData;

    [SerializeField] private TextMeshProUGUI _name; // LOCALE
    [SerializeField] private TextMeshProUGUI _price;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _background;
    
    public void Initialize()
    {
        _name.text = ModData.ModificatorName;
        _price.text = ModData.Price.ToString();
        _icon.sprite = ModData.Icon;
        _background.sprite = ModData.Background;

        var trigger = GetComponent<ModTooltipTrigger>();
        if (trigger == null) trigger = gameObject.AddComponent<ModTooltipTrigger>();
        trigger.SetData(ModData);
    }
}