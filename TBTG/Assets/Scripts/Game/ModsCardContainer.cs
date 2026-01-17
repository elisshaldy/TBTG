using UnityEngine;

public class ModsCardContainer : MonoBehaviour
{
    public ModData[] _mods = new ModData[6];

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

        // Перевіряємо чи є вільний слот і чи не перевищуємо ліміт
        return freeSlot >= 0 && currentPrice + modData.Price <= 5;
    }

    public void AddMod(ModData modData)
    {
        for (int i = 0; i < _mods.Length; i++)
        {
            if (_mods[i] == null)
            {
                _mods[i] = modData;
                return;
            }
        }
    }

    public void RemoveMod(ModData modData)
    {
        for (int i = 0; i < _mods.Length; i++)
        {
            if (_mods[i] == modData)
            {
                _mods[i] = null;
                return;
            }
        }
    }
}