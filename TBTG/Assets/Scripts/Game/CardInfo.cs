using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardInfo : MonoBehaviour
{
    public CharacterData CharData;

    [SerializeField] private LocalizationLabel _charName;
    [SerializeField] private Image _charImage;
    
    [SerializeField] private LocalizationLabel _attackBaseTxt;
    [SerializeField] private LocalizationLabel _defenseBaseTxt;

    [Header("Attack Grid UI")]
    [SerializeField] private Image[] _gridCells;
    [SerializeField] private Color _activeCellColor;
    [SerializeField] private Color _inactiveCellColor;
    [SerializeField] private Color _characterCellColor;

    public void Initialize()
    {
        _charName.Text.text = CharData.CharacterName;
        _charImage.sprite = CharData.CharacterSprite;
        _attackBaseTxt.SetSuffix($": {CharData.AttackBase}");
        _defenseBaseTxt.SetSuffix($": {CharData.DefenseBase}");

        UpdateAttackGrid();
    }

    private void UpdateAttackGrid()
    {
        if (CharData.AttackPatternGrid == null) return;
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
            // Unity's UI grid is usually top-to-bottom, so y = 0 is the top row.
            // Our Cell array is also usually indexed this way if we use y * 3 + x.

            if (CharData.AttackPatternGrid.IsCharacterTile(x, y))
            {
                _gridCells[i].color = _characterCellColor;
            }
            else if (CharData.AttackPatternGrid.Cells[i])
            {
                _gridCells[i].color = _activeCellColor;
            }
            else
            {
                _gridCells[i].color = _inactiveCellColor;
            }
        }
    }
}