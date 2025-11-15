using System.Collections.Generic;
using UnityEngine;

public class PairSystem : MonoBehaviour
{
    private Dictionary<CharacterData, CharacterData> _activeReservePairs = new Dictionary<CharacterData, CharacterData>();
    private Dictionary<CharacterData, bool> _oneTimeSwapUsed = new Dictionary<CharacterData, bool>();

    public void SwapPair(CharacterData activeChar)
    {
        // Логіка заміни активний/резервний
    }
}