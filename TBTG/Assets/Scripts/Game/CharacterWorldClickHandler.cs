using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CharacterWorldClickHandler : MonoBehaviour, IPointerClickHandler
{
    private int _ownerID;
    private int _pairID;
    private CharacterData _charData;

    public void Initialize(int ownerID, int pairID, CharacterData charData)
    {
        _ownerID = ownerID;
        _pairID = pairID;
        _charData = charData;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (CharacterPlacementManager.Instance != null)
            {
                CharacterPlacementManager.Instance.ToggleBigCard(_ownerID, _pairID, _charData);
            }
        }
    }
}
