using UnityEngine;
using System.Collections.Generic;

// ScriptableObject, що збер≥гаЇ склад персонаж≥в гравц€.
// …ого можна буде створювати в Assets дл€ кожного гравц€ (наприклад, Player1_Hand).
[CreateAssetMenu(fileName = "NewPlayerHand", menuName = "Game Data/Player Hand")]
public class PlayerHandData : ScriptableObject
{
    [Tooltip("—писок ус≥х об'Їкт≥в CharacterData, €к≥ гравець обрав дл€ гри.")]
    public List<CharacterData> SelectedCharacters = new List<CharacterData>();

    // !!! ¬»ѕ–ј¬Ћ≈ЌЌя CS1061: ƒќƒјЌќ DiscardedCharacters !!!
    [Tooltip("—писок карт, €к≥ були доступн≥ в драфт≥, але не були обран≥ (п≥дуть у в≥дб≥й).")]
    public List<CharacterData> DiscardedCharacters = new List<CharacterData>();

    // !!! ¬»ѕ–ј¬Ћ≈ЌЌя CS1061: ƒќƒјЌќ ClearHand() !!!
    /// <summary>
    /// ќчищаЇ обидва списки в руц≥. ¬икликаЇтьс€ на початку нового драфту.
    /// </summary>
    public void ClearHand()
    {
        SelectedCharacters.Clear();
        DiscardedCharacters.Clear();
        Debug.Log("PlayerHandData: Hand and Discards cleared.");
    }

    // ѕрим≥тка: якщо вам потр≥бн≥ будуть пари (2 CharacterData), ви можете створити тут 
    // спец≥альну List<CharacterPair> структуру п≥зн≥ше.

    // ¬важаЇмо, що дл€ 8 карток вам потр≥бно 8 CharacterData.
}