using UnityEngine;
using System.Collections.Generic;
using System.Linq;
// ПРИМІТКА: Вам потрібно переконатися, що enum 'SelectionMode'
// тепер існує як глобальний тип у файлі SelectionMode.cs!

// ScriptableObject, що зберігає склад персонажів гравця.
[CreateAssetMenu(fileName = "NewPlayerHand", menuName = "Game Data/Player Hand")]
public class PlayerHandData : ScriptableObject
{
    [Tooltip("Список усіх об'єктів CharacterData, які гравець обрав для гри.")]
    public List<CharacterData> SelectedCharacters = new List<CharacterData>();

    [Tooltip("Картки, які були відкинуті гравцем під час драфту (більше не доступні).")]
    public List<CharacterData> DiscardedCharacters = new List<CharacterData>();

    // Зберігає (CharacterData, SelectionMode) — режими вибору (Visible/Hidden).
    // Тепер SelectionMode — це глобальний тип, а не вкладений.
    [System.NonSerialized] // Це runtime дані, не зберігаємо в ассеті
    private List<(CharacterData data, SelectionMode mode)> _selectionModes =
        new List<(CharacterData data, SelectionMode mode)>();


    /// <summary>
    /// Очищає всі списки перед початком нового драфту.
    /// </summary>
    public void ClearHand()
    {
        SelectedCharacters.Clear();
        // DiscardedCharacters.Clear(); // Залишаємо відкинуті, щоб вони не з'явилися знову для P2!
        _selectionModes.Clear();
    }

    /// <summary>
    /// Встановлює режими вибору для кожного персонажа.
    /// </summary>
    public void SetSelectionModes(List<(CharacterData data, SelectionMode mode)> modes)
    {
        _selectionModes = modes;
    }

    /// <summary>
    /// Повертає режим вибору для конкретного персонажа.
    /// </summary>
    public SelectionMode GetSelectionMode(CharacterData data)
    {
        // Повертаємо режим, або None, якщо карта не знайдена в списку
        var found = _selectionModes.FirstOrDefault(x => x.data == data);
        return (found.data != null) ? found.mode : SelectionMode.None;
    }
}