// MovementCardData.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMoveCard", menuName = "Tactics/Movement Card")]
public class MovementCardData : ScriptableObject
{
    public string CardName;
    [TextArea]
    public string Description;

    [Header("Movement Pattern")]
    // Список відносних координат (X, Y) від поточної позиції персонажа.
    // Кожен Vector2Int представляє ОДНУ можливу кінцеву клітинку.
    public List<Vector2Int> RelativeDestinations;

    // Якщо true, рух може бути застосований лише у фіксованому напрямку (наприклад, тільки вперед).
    public bool Directional = true;

    [Header("Special Properties")]
    // Наприклад, для 'Teleport' можна пропустити перевірку прохідності клітинок на шляху
    public bool IgnoresPathObstacles = false;
}