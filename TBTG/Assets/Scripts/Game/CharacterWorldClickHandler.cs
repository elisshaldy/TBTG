using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CharacterWorldClickHandler : MonoBehaviour, IPointerClickHandler
{
    private int _ownerID;
    private int _pairID;
    private CharacterData _charData;

    public event System.Action<int, int> OnCharacterHidden;

    public void Initialize(int ownerID, int pairID, CharacterData charData)
    {
        _ownerID = ownerID;
        _pairID = pairID;
        _charData = charData;
    }

    private void OnDisable()
    {
        OnCharacterHidden?.Invoke(_ownerID, _pairID);
    }

    private void OnDestroy()
    {
        OnCharacterHidden?.Invoke(_ownerID, _pairID);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (CharacterPlacementManager.Instance != null && InitiativeSystem.Instance != null && InitiativeSystem.Instance.IsFinalized)
            {
                // If we are NOT in movement mode, check if this character is on a tile that is under attack
                if (!CharacterPlacementManager.Instance.IsMovementModeActive)
                {
                    Vector2Int myGridPos = CharacterPlacementManager.Instance.GetCharacterGridPos(_ownerID, _pairID);
                    if (myGridPos.x != -1 && CharacterPlacementManager.Instance.IsTileUnderAttack(myGridPos))
                    {
                        CharacterPlacementManager.Instance.TryAttackTile(myGridPos);
                        return; // Done
                    }
                }

                // Default: toggle info
                CharacterPlacementManager.Instance.ToggleBigCard(_ownerID, _pairID, _charData);
            }
        }
    }
}
