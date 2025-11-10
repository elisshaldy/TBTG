// TraitData.cs
using UnityEngine;

public abstract class TraitData : ScriptableObject
{
    [Header("Trait Economy")]
    public string TraitName;
    [TextArea]
    public string Description;
    public int Cost; // 1, 2, or 3 UP

    // Цей метод буде викликатися, коли риса вперше стає "відкритою"
    public virtual void RevealTrait()
    {
        Debug.Log($"Trait {TraitName} is now visible (Passive or Reactive activation).");
    }
}