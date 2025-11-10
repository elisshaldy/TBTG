using UnityEngine;
using System.Collections.Generic;

// ScriptableObject, що збер≥гаЇ склад персонаж≥в гравц€.
// …ого можна буде створювати в Assets дл€ кожного гравц€ (наприклад, Player1_Hand).
[CreateAssetMenu(fileName = "NewPlayerHand", menuName = "Game Data/Player Hand")]
public class PlayerHandData : ScriptableObject
{
    [Tooltip("—писок ус≥х об'Їкт≥в CharacterData, €к≥ гравець обрав дл€ гри.")]
    public List<CharacterData> SelectedCharacters = new List<CharacterData>();

    // ѕрим≥тка: якщо вам потр≥бн≥ будуть пари (2 CharacterData), ви можете створити тут 
    // спец≥альну List<CharacterPair> структуру п≥зн≥ше.

    // ¬важаЇмо, що дл€ 8 карток вам потр≥бно 8 CharacterData.
}