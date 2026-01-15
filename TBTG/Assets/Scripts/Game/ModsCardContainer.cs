using UnityEngine;

public class ModsCardContainer : MonoBehaviour
{
    public ModData[] _mods = new ModData[6];

    public bool TryAddMod(ModData modData)
    {
        int currentPrice = 0;
        foreach (var mod in _mods)
        {
            if (mod != null)
            {
                currentPrice += mod.Price;
            }
        }

        if (currentPrice + modData.Price > 5)
        {
            return false;
        }

        for (int i = 0; i < _mods.Length; i++)
        {
            if (_mods[i] == null)
            {
                _mods[i] = modData;
                return true;
            }
        }
        return false;
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