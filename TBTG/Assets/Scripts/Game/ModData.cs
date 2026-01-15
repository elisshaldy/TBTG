using UnityEngine;

[CreateAssetMenu(fileName = "NewMod", menuName = "Game Data/Mod")]
public class ModData : ScriptableObject
{
    public string ModificatorName;
    public string ModificatorDescription;
    public Sprite Icon;
    public ModType ModType;
    [Range(1, 5)] public int Price = 1;
}

public enum ModType
{
    Undefined,
    Active,
    Passive
}