using UnityEngine;
using System.Collections.Generic;

// ScriptableObject, що зберігає посилання на ВСІ доступні карти персонажів у грі.
[CreateAssetMenu(fileName = "MasterDeck", menuName = "Custom/Master Deck")]
public class MasterDeckData : ScriptableObject
{
    [Tooltip("Список усіх доступних персонажів у грі.")]
    public List<CharacterData> AllAvailableCharacters = new List<CharacterData>();
}