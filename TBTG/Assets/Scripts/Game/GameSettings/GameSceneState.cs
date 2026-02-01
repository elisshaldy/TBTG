using UnityEngine;
using TMPro;
using Photon.Pun;
using System;

public class GameSceneState : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _gameStateText;

    [SerializeField] private SceneState _sceneState;

    [Header("Settings")]
    [SerializeField] private MultiplayerSettings multiplayerSettings;
    [SerializeField] private HotseatSettings hotseatSettings;
    [SerializeField] private PlayerVsBotSettings playerVsBotSettings;

    public GameSettings _currentSettings;
    private int _flowIndex = 0;

    public GameSetupStep CurrentStep
    {
        get
        {
            var flow = _currentSettings.GetFlow();
            if (flow != null && _flowIndex >= 0 && _flowIndex < flow.Length)
                return flow[_flowIndex];
            return GameSetupStep.Cards;
        }
    }

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
        _flowIndex = 0;
        var flow = _currentSettings.GetFlow();
        if (flow.Length > 0)
        {
            _currentSettings.PrepareStep(flow[0], ui);
            ui.EnableDeckListening();
            _currentSettings.OnFlowStarted(ui);
            ui.OpenCards();
        }
    }

    public void Next(GameUIController ui)
    {
        var flow = _currentSettings.GetFlow();
        _flowIndex++;

        if (_flowIndex >= flow.Length) return;

        GameSetupStep nextStep = flow[_flowIndex];
        //Debug.Log($"GameSceneState: Switching to {nextStep} step");

        // 1. Готуємо дані для кроку
        _currentSettings.PrepareStep(nextStep, ui);

        // 2. Виконуємо UI логіку
        switch (nextStep)
        {
            case GameSetupStep.Cards:
                ui.OpenCards();
                break;

            case GameSetupStep.Mods:
                ui.OpenMods();
                break;

            case GameSetupStep.ModeSpecific:
                _currentSettings.OpenModeSpecific(ui);
                break;
            
            case GameSetupStep.Map:
                ui.OpenMap();
                _currentSettings.OnFlowFinished(ui);
                break;
        }
    }

    private void SelectSettings()
    {
        if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.CurrentSettings != null)
        {
            _sceneState = GameSettingsManager.Instance.CurrentMode;
        }
        else if (PhotonNetwork.InRoom)
        {
            _sceneState = SceneState.Multiplayer;
        }

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

        if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.CurrentSettings != null)
        {
            CopyValues(GameSettingsManager.Instance.CurrentSettings, _currentSettings);
        }

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
        target.InfluenceInitiative = source.InfluenceInitiative;

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
        if (props.TryGetValue("CharIndices", out object charIndices)) _currentSettings.CharacterPoolIndices = (int[])charIndices;
        if (props.TryGetValue("Initiative", out object initiative)) _currentSettings.InfluenceInitiative = Convert.ToBoolean(initiative);
        
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
        if (GameSettingsManager.Instance != null)
        {
            _gameStateText.gameObject.SetActive(GameSettingsManager.Instance.IsDebug);
        }

        if (_sceneState == SceneState.Undefined || _currentSettings == null)
        {
            _gameStateText.text = "Mode: Undefined\nGame settings are not initialized.";
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Mode: {_sceneState}");
        sb.AppendLine($"Turn Time: {_currentSettings.TurnTime}");
        sb.AppendLine($"Field Size: {_currentSettings.FieldSize}x{_currentSettings.FieldSize}");
        sb.AppendLine($"Bosses: {_currentSettings.BossCount}");
        sb.AppendLine($"Party Count: {_currentSettings.PartyCount}");
        sb.AppendLine($"Boss Difficulty: {_currentSettings.BossDifficulty}");
        sb.AppendLine($"Initiative: {_currentSettings.InfluenceInitiative}");

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
            foreach (var player in mp.PlayerList) sb.AppendLine($"- {player}");
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