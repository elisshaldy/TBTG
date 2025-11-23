using UnityEngine;
using System.Collections.Generic;

// ScriptableObject, �� ������ ��������� �� �Ѳ �������� ����� ��������� � ��.
[CreateAssetMenu(fileName = "MasterDeck", menuName = "Custom/Master Deck")]
public class MasterDeckData : ScriptableObject
{
    [Tooltip("������ ��� ��������� ��������� � ��.")]
    public List<CharacterData> AllAvailableCharacters = new List<CharacterData>();

    [Tooltip("Список всіх доступних рис у грі (з них генерується пул з 35 для купівлі).")]
    public List<TraitData> AllAvailableTraits = new List<TraitData>();
}