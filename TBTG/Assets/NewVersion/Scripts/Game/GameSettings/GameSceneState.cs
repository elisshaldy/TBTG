using UnityEngine;
using TMPro;
using Photon.Pun;
using System;

public enum SceneState
{
    Undefined,
    Multiplayer,
    Hotseat,
    PlayerVSBot
}

public class GameSceneState : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _gameStateText;

    [SerializeField] private SceneState _sceneState;

    [Header("Settings")]
    [SerializeField] private MultiplayerSettings multiplayerSettings;
    [SerializeField] private HotseatSettings hotseatSettings;
    [SerializeField] private PlayerVsBotSettings playerVsBotSettings;

    public GameSettings _currentSettings;
    private GameSetupStep _currentStep = GameSetupStep.Cards;
    public GameSetupStep CurrentStep => _currentStep;

    private void Awake()
    {
        SelectSettings();
    }

    private void Start()
    {
        UpdateUI();
    }

    public void StartFlow(GameUIController ui)
    {
        _currentStep = GameSetupStep.Cards;
        ui.EnableDeckListening();
        _currentSettings.OnFlowStarted(ui);
        ui.OpenCards();
    }

    public void Next(GameUIController ui)
    {
        _currentStep++;

        switch (_currentStep)
        {
            case GameSetupStep.Mods:
                Debug.Log($"GameSceneState: Switching to Mods step");
                ui.OpenMods();
                _currentSettings.OnFlowFinished(ui);
                break;

            case GameSetupStep.ModeSpecific:
                _currentSettings.OpenModeSpecific(ui);
                break;
        }
    }

    private void SelectSettings()
    {
        // Спочатку визначаємо режим
        if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.CurrentSettings != null)
        {
            _sceneState = GameSettingsManager.Instance.CurrentMode;
        }
        else if (PhotonNetwork.InRoom)
        {
            _sceneState = SceneState.Multiplayer;
        }

        // Призначаємо _currentSettings на один з готових об'єктів в інспекторі
        switch (_sceneState)
        {
            case SceneState.Multiplayer: _currentSettings = multiplayerSettings; break;
            case SceneState.Hotseat: _currentSettings = hotseatSettings; break;
            case SceneState.PlayerVSBot: _currentSettings = playerVsBotSettings; break;
        }

        if (_currentSettings == null)
        {
            Debug.LogError("Failed to identify GameSettings to use!");
            return;
        }

        // Копіюємо дані з менеджера (якщо вони там є)
        if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.CurrentSettings != null)
        {
            CopyValues(GameSettingsManager.Instance.CurrentSettings, _currentSettings);
        }

        // Якщо мультіплеєр — докачуємо дані з Photon
        if (_sceneState == SceneState.Multiplayer && PhotonNetwork.InRoom)
        {
            SyncMultiplayerSettings();
        }
    }

    private void CopyValues(GameSettings source, GameSettings target)
    {
        target.TurnTime = source.TurnTime;
        target.FieldSize = source.FieldSize;
        target.BossCount = source.BossCount;
        target.PartyCount = source.PartyCount;
        target.BossDifficulty = source.BossDifficulty;

        if (source is PlayerVsBotSettings sBot && target is PlayerVsBotSettings tBot)
            tBot.BotDifficulty = sBot.BotDifficulty;
        
        if (source is HotseatSettings sHs && target is HotseatSettings tHs)
        {
            tHs.Player1Name = sHs.Player1Name;
            tHs.Player2Name = sHs.Player2Name;
        }
    }

    private void SyncMultiplayerSettings()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        
        if (props.TryGetValue("TurnTime", out object turnTime)) _currentSettings.TurnTime = Convert.ToInt32(turnTime);
        if (props.TryGetValue("FieldSize", out object fieldSize)) _currentSettings.FieldSize = Convert.ToInt32(fieldSize);
        if (props.TryGetValue("PartyCount", out object partyCount)) _currentSettings.PartyCount = Convert.ToInt32(partyCount);
        if (props.TryGetValue("BossCount", out object bossCount)) _currentSettings.BossCount = Convert.ToInt32(bossCount);
        if (props.TryGetValue("BossDifficulty", out object bossDiff)) _currentSettings.BossDifficulty = (BossDifficulty)Convert.ToInt32(bossDiff);
        
        if (_currentSettings is MultiplayerSettings mp)
        {
            mp.RoomName = PhotonNetwork.CurrentRoom.Name;
            mp.YourName = PhotonNetwork.LocalPlayer.NickName;
            mp.PlayerList = new System.Collections.Generic.List<string>();
            foreach (var p in PhotonNetwork.PlayerList)
            {
                mp.PlayerList.Add(p.NickName);
            }
        }
    }

    private void UpdateUI()
    {
        if (_sceneState == SceneState.Undefined || _currentSettings == null)
        {
            _gameStateText.text =
                "Mode: Undefined\n" +
                "Game settings are not initialized.";
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Mode: {_sceneState}");
        sb.AppendLine($"Turn Time: {_currentSettings.TurnTime}");
        sb.AppendLine($"Field Size: {_currentSettings.FieldSize}x{_currentSettings.FieldSize}");
        sb.AppendLine($"Bosses: {_currentSettings.BossCount}");
        sb.AppendLine($"Party Count: {_currentSettings.PartyCount}");
        sb.AppendLine($"Boss Difficulty: {_currentSettings.BossDifficulty}");

        if (_currentSettings is PlayerVsBotSettings botSettings)
        {
            sb.AppendLine($"Bot Difficulty: {botSettings.BotDifficulty}");
        }

        if (_currentSettings is MultiplayerSettings mp)
        {
            sb.AppendLine();
            sb.AppendLine($"Room Name: {mp.RoomName}");
            sb.AppendLine($"Your Name: {mp.YourName}");
            sb.AppendLine("Players:");

            foreach (var player in mp.PlayerList)
            {
                sb.AppendLine($"- {player}");
            }
        }

        if (_currentSettings is HotseatSettings hs)
        {
            sb.AppendLine();
            sb.AppendLine($"Player 1: {hs.Player1Name}");
            sb.AppendLine($"Player 2: {hs.Player2Name}");
        }

        _gameStateText.text = sb.ToString();
    }
}