using UnityEngine;
using UnityEngine.UI;

public class MovementCardInfo : MonoBehaviour
{
    public MovementCard MoveCard;

    [SerializeField] private LocalizationLabel _cardName;
    [SerializeField] private Image _cardImage;
    
    [Header("Movement Grid UI")]
    [SerializeField] private Sprite _char;
    [SerializeField] private Sprite _inactiveCell;
    [SerializeField] private Sprite _activeCell;

    [SerializeField] private Image[] _gridCells;

    public void Initialize()
    {
        // _cardName.SetKey(MoveCard.CardName); // Only if CardName exists, user removed it?
        // User removed CardName and CardSprite from MovementCard.cs in the diff?
        // Let's check the diff again.
        // Yes, user removed CardName and CardSprite.
        
        //UpdateMovementGrid();
    }

    private void UpdateMovementGrid()
    {
        if (MoveCard.MovementPatternGrid == null) return;
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
}
