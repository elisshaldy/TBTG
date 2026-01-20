using UnityEngine;
using UnityEngine.UI;

public class ModsCardContainer : MonoBehaviour
{
    public ModData[] _mods = new ModData[6];
    
    [SerializeField] private Image[] _modsIcon = new Image[6]; // 2x6

    private void Awake()
    {
        for (int i = 0; i < _modsIcon.Length; i++)
        {
            if (_modsIcon[i] == null) continue;
            
            var btn = _modsIcon[i].GetComponent<Button>();
            if (btn == null) btn = _modsIcon[i].gameObject.AddComponent<Button>();
            
            int index = i;
            btn.onClick.AddListener(() => OnIconClick(index));
            
            _modsIcon[i].gameObject.SetActive(false);
        }
    }

    private void OnIconClick(int index)
    {
        // Шукаємо вимкнений об'єкт модифікатора всередині слота іконки
        var dragHandler = _modsIcon[index].GetComponentInChildren<ModDragHandler>(true);
        if (dragHandler != null)
        {
            dragHandler.DetachFromCard();
        }
    }

    public bool TryAddMod(ModData modData)
    {
        if (!CanAddMod(modData))
            return false;
            
        AddMod(modData);
        return true;
    }

    public bool CanAddMod(ModData modData)
    {
        int currentPrice = 0;
        int freeSlot = -1;
        
        for (int i = 0; i < _mods.Length; i++)
        {
            if (_mods[i] != null)
            {
                currentPrice += _mods[i].Price;
            }
            else if (freeSlot < 0)
            {
                freeSlot = i;
            }
        }

        return freeSlot >= 0 && currentPrice + modData.Price <= 5;
    }

    public Transform AddMod(ModData modData)
    {
        for (int i = 0; i < _mods.Length; i++)
        {
            if (_mods[i] == null)
            {
                _mods[i] = modData;
                if (_modsIcon[i] != null)
                {
                    _modsIcon[i].sprite = modData.Icon;
                    _modsIcon[i].gameObject.SetActive(true);
                    return _modsIcon[i].transform;
                }
                return transform;
            }
        }
        return null;
    }

    public void RemoveMod(ModData modData)
    {
        for (int i = 0; i < _mods.Length; i++)
        {
            if (_mods[i] == modData)
            {
                _mods[i] = null;
                if (_modsIcon[i] != null)
                {
                    _modsIcon[i].sprite = null;
                    _modsIcon[i].gameObject.SetActive(false);
                }
                return;
            }
        }
    }
}