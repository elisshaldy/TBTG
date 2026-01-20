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
        int targetIndex = GetFreeIndexForType(modData.ModType);
        
        if (targetIndex >= 0)
        {
            _mods[targetIndex] = modData;
            if (_modsIcon[targetIndex] != null)
            {
                _modsIcon[targetIndex].sprite = modData.Icon;
                _modsIcon[targetIndex].gameObject.SetActive(true);
                return _modsIcon[targetIndex].transform;
            }
        }
        return null;
    }

    private int GetFreeIndexForType(ModType type)
    {
        if (type == ModType.Active)
        {
            // Активні йдуть зліва направо: 0, 1, 2...
            for (int i = 0; i < 3; i++)
            {
                if (_mods[i] == null) return i;
            }
        }
        else
        {
            // Пасивні йдуть справа наліво: 5, 4, 3...
            for (int i = 5; i >= 3; i--)
            {
                if (_mods[i] == null) return i;
            }
        }
        return -1;
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