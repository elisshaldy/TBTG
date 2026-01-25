using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class GameSettings
{
    public int TurnTime;
    public int FieldSize;
    public int BossCount;
    public int PartyCount;
    public int[] CharacterPoolIndices; // Спільний список індексів для всіх гравців
    public BossDifficulty BossDifficulty;

    public int[] GetIndicesForPlayer(int playerIndex)
    {
        if (CharacterPoolIndices == null || CharacterPoolIndices.Length == 0) return null;
        
        int countPerPlayer = 10;
        int start = playerIndex * countPerPlayer;
        
        if (start >= CharacterPoolIndices.Length) return null;

        int actualCount = Mathf.Min(countPerPlayer, CharacterPoolIndices.Length - start);
        int[] result = new int[actualCount];
        System.Array.Copy(CharacterPoolIndices, start, result, 0, actualCount);
        return result;
    }

    public virtual void OnFlowStarted(GameUIController ui) {}
    public abstract void OpenModeSpecific(GameUIController ui);
    public virtual void OnFlowFinished(GameUIController ui) {}
}

public enum BossDifficulty
{
    Undefined,
    Easy,
    Normal,
    Hard
}

public enum BotDifficulty
{
    Undefined,
    Easy,
    Normal,
    Hard
}

public enum GameSetupStep
{
    Cards,
    Mods,
    ModeSpecific
}

[Serializable]
public class MultiplayerSettings : GameSettings
{
    public string RoomName;
    public string YourName;
    public List<string> PlayerList;
    
    public override void OpenModeSpecific(GameUIController ui)
    {
        ui.OpenMap();
    }
    
    public override void OnFlowFinished(GameUIController ui)
    {
        //ui.DisableDeckListening();
    }
}

[Serializable]
public class PlayerVsBotSettings : GameSettings
{
    public BotDifficulty BotDifficulty;
    
    public override void OpenModeSpecific(GameUIController ui)
    {
        ui.OpenMap();
    }
    
    public override void OnFlowFinished(GameUIController ui)
    {
        //ui.DisableDeckListening();
    }
}

[Serializable]
public class HotseatSettings : GameSettings
{
    public string Player1Name;
    public string Player2Name;
    
    public override void OnFlowStarted(GameUIController ui)
    {
        ui.ShowHotseatPlayer(Player1Name); // show player 1 name
    }
    
    public override void OpenModeSpecific(GameUIController ui)
    {
        ui.OpenHotseatWindow(); // show player 2 name
    }
}