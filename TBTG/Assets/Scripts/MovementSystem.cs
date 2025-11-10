// MovementSystem.cs
using UnityEngine;
using System.Collections.Generic;

public class MovementSystem : MonoBehaviour
{
    private Character _character;

    void Awake()
    {
        _character = GetComponent<Character>();
    }

    // ----------------------------------------------------------------------
    // ОСНОВНА ФУНКЦІЯ: РОЗРАХУНОК МОЖЛИВИХ КЛІТИНОК
    // ----------------------------------------------------------------------
    public List<Vector2Int> GetAvailableMoves(MovementCardData card)
    {
        List<Vector2Int> possibleDestinations = new List<Vector2Int>();
        Vector2Int currentPos = _character.GridPosition;

        foreach (Vector2Int relativeMove in card.RelativeDestinations)
        {
            Vector2Int destination = currentPos + relativeMove;

            // 1. Перевірка на межі поля
            if (!IsPositionValid(destination)) continue;

            Tile destinationTile = GridManager.Instance.GetTile(destination);
            if (destinationTile == null) continue;

            // 2. Перевірка прохідності кінцевої клітинки
            if (!destinationTile.IsPassable()) continue;

            // 3. Перевірка наявності перешкод на ШЛЯХУ (якщо це не телепорт)
            if (!card.IgnoresPathObstacles && !CheckPathPassability(currentPos, destination)) continue;

            possibleDestinations.Add(destination);
        }

        return possibleDestinations;
    }

    // ----------------------------------------------------------------------
    // ДОПОМІЖНІ ФУНКЦІЇ ПЕРЕВІРКИ
    // ----------------------------------------------------------------------

    private bool IsPositionValid(Vector2Int pos)
    {
        // Перевірка, чи знаходиться позиція в межах GridManager
        return pos.x >= 0 && pos.x < GridManager.Instance.MapWidth &&
               pos.y >= 0 && pos.y < GridManager.Instance.MapHeight;
    }

    private bool CheckPathPassability(Vector2Int start, Vector2Int end)
    {
        // Це спрощений код: для "Dash 3" потрібно перевірити 1-у та 2-у клітинки
        // Для L-подібного руху або Pivot+Step потрібна більш складна логіка.
        // Для MVP можна перевіряти лише кінцеву клітинку, якщо хід простий.

        // Тут потрібен повноцінний алгоритм перевірки лінії або покрокового шляху.
        // Наприклад, A* для визначення шляху або Raycasting для прямої лінії.

        // Для прямого ходу (коли |dX| = 0 або |dY| = 0)
        Vector2Int direction = end - start;
        int steps = Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));

        for (int i = 1; i < steps; i++)
        {
            Vector2Int intermediatePos = start + new Vector2Int(
                (int)Mathf.Sign(direction.x) * (i * (Mathf.Abs(direction.x) > 0 ? 1 : 0)),
                (int)Mathf.Sign(direction.y) * (i * (Mathf.Abs(direction.y) > 0 ? 1 : 0))
            );

            Tile intermediateTile = GridManager.Instance.GetTile(intermediatePos);
            // Перевірка, чи не заважає стіна
            if (intermediateTile != null && intermediateTile.Type == TileType.Impassable)
            {
                return false;
            }
        }

        return true;
    }

    // ----------------------------------------------------------------------
    // ВИКОНАННЯ РУХУ
    // ----------------------------------------------------------------------
    public void MoveCharacter(Vector2Int destination, MovementCardData cardUsed, MovementDeckManager deckManager, bool isDoubleMove)
    {
        // Оновлення клітинки
        Tile oldTile = GridManager.Instance.GetTile(_character.GridPosition);
        Tile newTile = GridManager.Instance.GetTile(destination);

        if (oldTile != null) oldTile.RemoveOccupant();
        if (newTile != null) newTile.SetOccupant(_character);

        // Оновлення позиції персонажа
        _character.GridPosition = destination;
        _character.transform.position = newTile.transform.position; // Фізичне переміщення

        // Логіка колоди
        if (isDoubleMove)
        {
            // У GDD вказано, що при подвійному русі "одна з них 'вигорає'"
            deckManager.UseCard(cardUsed, true);
        }
        else
        {
            deckManager.UseCard(cardUsed, false);
        }
    }
}