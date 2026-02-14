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
    public int[] MovementPoolIndices;
    public BossDifficulty BossDifficulty;
    public bool InfluenceInitiative;

    public int[] GetIndicesForPlayer(int playerIndex)
    {
        if (CharacterPoolIndices == null || CharacterPoolIndices.Length == 0) return null;
        
        int countPerPlayer = 10;
        int start = (playerIndex - 1) * countPerPlayer;
        
        if (start < 0 || start >= CharacterPoolIndices.Length) 
        {
            // If we don't have enough specific indices, return null to trigger random fallback
            return null;
        }

        int actualCount = Mathf.Min(countPerPlayer, CharacterPoolIndices.Length - start);
        int[] result = new int[actualCount];
        System.Array.Copy(CharacterPoolIndices, start, result, 0, actualCount);
        return result;
    }

    public int[] GetMovementIndicesForPlayer(int playerIndex)
    {
        if (MovementPoolIndices == null || MovementPoolIndices.Length == 0) return null;
        
        int countPerPlayer = 6; // Matching _movementCardsToSpawn in Initializer
        int start = (playerIndex - 1) * countPerPlayer;
        
        if (start < 0 || start >= MovementPoolIndices.Length) 
        {
            // Fallback: if we need indices for player 1 but only have some for player 0, 
            // returning null here will trigger the random fallback in Initializer
            return null;
        }

        int actualCount = Mathf.Min(countPerPlayer, MovementPoolIndices.Length - start);
        int[] result = new int[actualCount];
        System.Array.Copy(MovementPoolIndices, start, result, 0, actualCount);
        return result;
    }

    public abstract GameSetupStep[] GetFlow();
    public virtual void PrepareStep(GameSetupStep step, GameUIController ui) {}

    public virtual void OnFlowStarted(GameUIController ui) {}
    public virtual int CurrentPlayerIndex => 1; // Default to P1
    public virtual void TakeSnapshot(List<GameObject> selectedCards) {}
    public virtual void RegisterMovementCards(int playerIndex, List<GameObject> movementCards) {}
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

    public PlayerSnapshot LocalPlayerSnapshot = new PlayerSnapshot();
    public PlayerSnapshot RemotePlayerSnapshot = new PlayerSnapshot();
    
    public override int CurrentPlayerIndex => Photon.Pun.PhotonNetwork.InRoom ? Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber : 1;

    public override void RegisterMovementCards(int playerIndex, List<GameObject> movementCards)
    {
        PlayerSnapshot target = (playerIndex == CurrentPlayerIndex) ? LocalPlayerSnapshot : RemotePlayerSnapshot;
        target.SelectedMovementCards = new List<GameObject>(movementCards);
        target.PlayerIndex = playerIndex;
    }

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
    
    public PlayerSnapshot PlayerSnapshot = new PlayerSnapshot();
    public PlayerSnapshot BotSnapshot = new PlayerSnapshot();

    public override void RegisterMovementCards(int playerIndex, List<GameObject> movementCards)
    {
        PlayerSnapshot target = (playerIndex == 1) ? PlayerSnapshot : BotSnapshot;
        target.SelectedMovementCards = new List<GameObject>(movementCards);
        target.PlayerIndex = playerIndex;
    }
    
    public override void OpenModeSpecific(GameUIController ui)
    {
        ui.OpenMap();
    }
}

[Serializable]
public class PlayerSnapshot
{
    public List<GameObject> SelectedCards = new List<GameObject>();
    public List<GameObject> SelectedMovementCards = new List<GameObject>();
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
        GameSetupStep.Cards, 
        GameSetupStep.Mods, 
        GameSetupStep.Map 
    };

    public override int CurrentPlayerIndex => _playerSelectionCycle == 0 ? 1 : _playerSelectionCycle;

    public override void TakeSnapshot(List<GameObject> selectedCards)
    {
        PlayerSnapshot target = (_playerSelectionCycle == 1) ? Player1Snapshot : Player2Snapshot;
        target.SelectedCards = new List<GameObject>(selectedCards);
        target.PlayerIndex = _playerSelectionCycle;
    }

    public override void RegisterMovementCards(int playerIndex, List<GameObject> movementCards)
    {
        PlayerSnapshot target = (playerIndex == 1) ? Player1Snapshot : Player2Snapshot;
        target.SelectedMovementCards = new List<GameObject>(movementCards);
        target.PlayerIndex = playerIndex;
    }

    public override void PrepareStep(GameSetupStep step, GameUIController ui)
    {
        if (step == GameSetupStep.Cards)
        {
            _playerSelectionCycle++;
            string currentName = (_playerSelectionCycle == 1) ? Player1Name : Player2Name;
            ui.ShowHotseatPlayer(currentName);
        }
    }
    
    public override void OnFlowFinished(GameUIController ui)
    {
        // 1. Ховаємо карти другого гравця (він щойно закінчив)
        if (Player2Snapshot != null && Player2Snapshot.SelectedCards != null)
        {
            foreach (var card in Player2Snapshot.SelectedCards)
            {
                if (card != null) card.SetActive(false);
            }
        }

        // 2. Вмикаємо карти першого гравця
        if (Player1Snapshot != null && Player1Snapshot.SelectedCards != null)
        {
            foreach (var card in Player1Snapshot.SelectedCards)
            {
                if (card != null) card.SetActive(true);
            }
        }

        // 3. Оновлюємо ім'я гравця на перший
        ui.ShowHotseatPlayer(Player1Name);
        Debug.Log($"Flow finished. Switched back to {Player1Name} for the game.");
    }

    public override void OpenModeSpecific(GameUIController ui)
    {
        ui.OpenHotseatWindow(); 
    }
}