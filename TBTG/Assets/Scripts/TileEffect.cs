// TileEffectData.cs (ScriptableObject для Активних Клітинок)
using UnityEngine;

[CreateAssetMenu(fileName = "NewTileEffect", menuName = "Tactics/Tile Effect")]
public class TileEffectData : ScriptableObject
{
    public string EffectName;
    [TextArea]
    public string EffectDescription;

    [Header("Combat Modifiers")]
    public int AttackAdvantageChange = 0; // -1 для переваги, +1 для перешкоди
    public int DefenseAdvantageChange = 0;

    [Header("Round End Effect")]
    public bool HealOnRoundEnd = false; // Одноразове лікування
    public bool WorsenStateOnRoundEnd = false; // Погіршення стану

    // Метод, який можна викликати, коли персонаж стає на клітинку
    public void ApplyInitialEffect(Character character)
    {
        // Наприклад, ігнорування першого Impact
    }
}