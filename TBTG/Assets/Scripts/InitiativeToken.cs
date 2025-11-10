// InitiativeToken.cs
using UnityEngine;

// Це може бути або ScriptableObject (для даних), або простий клас (для використання в пам'яті).
// Для простоти візьмемо простий клас, який створюється на початку раунду.
public class InitiativeToken
{
    public Character CharacterReference; // Посилання на персонажа, до якого відноситься хід
    public int PlayerID; // ID гравця (1 або 2)
    public bool IsRevealed = false;

    public InitiativeToken(Character character, int playerID)
    {
        CharacterReference = character;
        PlayerID = playerID;
    }
}