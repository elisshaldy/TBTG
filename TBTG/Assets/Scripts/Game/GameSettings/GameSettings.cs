using System;
using System.Collections.Generic;
using UnityEngine;

public enum SceneState
{
    Undefined,
    Multiplayer,
    Hotseat,
    PlayerVSBot
}

public enum GameSetupStep
{
    Cards,
    Mods,
    ModeSpecific,
    Map
}

[Serializable]
public abstract class GameSettings
{
    public int TurnTime;
    public int FieldSize;
    public int BossCount;
    public int PartyCount;
    public int[] CharacterPoolIndices;
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

    public abstract GameSetupStep[] GetFlow();
    public virtual void PrepareStep(GameSetupStep step, GameUIController ui) {}

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

[Serializable]
public class MultiplayerSettings : GameSettings
{
    public string RoomName;
    public string YourName;
    public List<string> PlayerList;

    public override GameSetupStep[] GetFlow() => new[] { 
        GameSetupStep.Cards, 
        GameSetupStep.Mods, 
        GameSetupStep.Map 
    };
    
    public override void OpenModeSpecific(GameUIController ui)
    {
        ui.OpenMap();
    }
}

[Serializable]
public class PlayerVsBotSettings : GameSettings
{
    public BotDifficulty BotDifficulty;

    public override GameSetupStep[] GetFlow() => new[] { 
        GameSetupStep.Cards, 
        GameSetupStep.Mods, 
        GameSetupStep.Map 
    };
    
    public override void OpenModeSpecific(GameUIController ui)
    {
        ui.OpenMap();
    }
}

[Serializable]
public class PlayerSnapshot
{
    public List<GameObject> SelectedCards = new List<GameObject>();
    public int ModPoints;
    public int PlayerIndex;
}

[Serializable]
public class HotseatSettings : GameSettings
{
    public string Player1Name;
    public string Player2Name;

    public PlayerSnapshot Player1Snapshot = new PlayerSnapshot();
    public PlayerSnapshot Player2Snapshot = new PlayerSnapshot();

    private int _playerSelectionCycle = 0;

    public override GameSetupStep[] GetFlow() => new[] { 
        GameSetupStep.Cards, 
        GameSetupStep.Mods, 
        GameSetupStep.ModeSpecific, 
        GameSetupStep.Cards, 
        GameSetupStep.Mods, 
        GameSetupStep.Map 
    };

    public override void PrepareStep(GameSetupStep step, GameUIController ui)
    {
        if (step == GameSetupStep.Cards)
        {
            _playerSelectionCycle++;
            string currentName = (_playerSelectionCycle == 1) ? Player1Name : Player2Name;
            ui.ShowHotseatPlayer(currentName);
        }
    }
    
    public override void OpenModeSpecific(GameUIController ui)
    {
        ui.OpenHotseatWindow(); 
    }
}