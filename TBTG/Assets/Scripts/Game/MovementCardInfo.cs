using UnityEngine;
using UnityEngine.UI;

public class MovementCardInfo : MonoBehaviour
{
    public MovementCard MoveCard;

    //[SerializeField] private LocalizationLabel _cardName;
    
    [Header("Movement Grid UI")]
    [SerializeField] private Sprite _char;
    [SerializeField] private Sprite _inactiveCell;
    [SerializeField] private Sprite _activeCell;

    [SerializeField] private Image[] _gridCells;
    [Header("UI Interaction")]
    [SerializeField] private Button _btn;

    public int OwnerID { get; set; } = -1;

    public void Initialize(int ownerID)
    {
        OwnerID = ownerID;
        UpdateMovementGrid();
        
        if (_btn == null) _btn = GetComponent<Button>();
        if (_btn != null)
        {
            _btn.onClick.RemoveAllListeners();
            _btn.onClick.AddListener(OnMovementCardClicked);
        }
    }

    private void OnMovementCardClicked()
    {
        if (CharacterPlacementManager.Instance == null || InitiativeSystem.Instance == null) return;

        int localPlayerID = 1;
        if (Photon.Pun.PhotonNetwork.InRoom) 
        {
            localPlayerID = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
        }
        else if (InitiativeSystem.Instance != null && InitiativeSystem.Instance.IsFinalized)
        {
            localPlayerID = InitiativeSystem.Instance.CurrentTurnPlayerID;
        }

        // STRICT OWNERSHIP: You can only click YOUR OWN cards
        if (OwnerID != localPlayerID)
        {
            Debug.Log($"[MovementCard] Not your card! This card belongs to Player {OwnerID}, but it's Player {localPlayerID}'s turn.");
            return;
        }

        // If this specific card is already active, toggle it OFF
        if (CharacterPlacementManager.Instance.IsMovementModeActive && 
            CharacterPlacementManager.Instance.ActiveMovementCard == MoveCard)
        {
            CharacterPlacementManager.Instance.ClearMovementMode();
        }
        else
        {
            // Otherwise, set THIS card as active (switching from another move card or from attack mode)
            CharacterPlacementManager.Instance.SetMovementMode(this);
        }
    }

    private void UpdateMovementGrid()
    {
        if (MoveCard == null || MoveCard.MovementPatternGrid == null) return;
        if (_gridCells == null || _gridCells.Length == 0)
        {
            Debug.LogWarning($"Grid cells not assigned in {gameObject.name}", gameObject);
            return;
        }

        for (int i = 0; i < 9; i++)
        {
            if (i >= _gridCells.Length) break;

            int x = i % 3;
            int y = i / 3;

            if (MoveCard.MovementPatternGrid.IsCharacterTile(x, y))
            {
                _gridCells[i].sprite = _char;
            }
            else if (MoveCard.MovementPatternGrid.Cells[i])
            {
                _gridCells[i].sprite = _activeCell;
            }
            else
            {
                _gridCells[i].sprite = _inactiveCell;
            }
        }
    }

    public void HidePattern()
    {
        if (_gridCells == null) return;
        foreach (var img in _gridCells)
        {
            if (img != null) img.gameObject.SetActive(false);
        }
    }
}
