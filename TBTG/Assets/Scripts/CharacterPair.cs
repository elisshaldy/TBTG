// CharacterPair.cs
using UnityEngine;
using System.Collections.Generic; // Потрібен для List<CharacterData>

[System.Serializable]
public class CharacterPair
{
    // *** РЕКОМЕНДОВАНІ НАЗВИ ПОЛІВ ***
    [Tooltip("Персонаж, який виходить на поле (Активна карта).")]
    public CharacterData ActiveCharacter;

    [Tooltip("Персонаж, який залишається в резерві (Прихована карта).")]
    public CharacterData HiddenCharacter;

    // Конструктор для зручного створення об'єкта
    public CharacterPair(CharacterData active, CharacterData hidden)
    {
        ActiveCharacter = active;
        HiddenCharacter = hidden;
    }

    /// <summary>
    /// Повертає обидва CharacterData в списку (Активний, потім Прихований).
    /// </summary>
    public List<CharacterData> GetAllCharacters()
    {
        List<CharacterData> characters = new List<CharacterData>();

        // Додаємо в порядку пріоритету
        if (ActiveCharacter != null) characters.Add(ActiveCharacter);
        if (HiddenCharacter != null) characters.Add(HiddenCharacter);

        return characters;
    }
}