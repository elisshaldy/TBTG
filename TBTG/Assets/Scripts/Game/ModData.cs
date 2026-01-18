using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "NewMod", menuName = "Game Data/Mod")]
public class ModData : ScriptableObject
{
    public string ModificatorName;
    public string ModificatorDescription;
    public Sprite Icon;
    public ModType ModType;
    [Range(1, 5)] public int Price = 1;

    public List<ReactiveParameter> ReactiveParameters = new List<ReactiveParameter>();

    public List<ModData> Critical = new List<ModData>();
}

public enum ModType
{
    Undefined,
    Active,
    Passive
}

[Serializable]
public class ReactiveParameter
{
    public int MinValue = 3;
    public int MaxValue = 18;
}