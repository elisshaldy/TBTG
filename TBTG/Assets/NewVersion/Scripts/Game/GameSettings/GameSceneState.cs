using UnityEngine;
using TMPro;

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

    private void Start()
    {
        SelectSettings();
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
        switch (_sceneState)
        {
            case SceneState.Multiplayer:
                _currentSettings = multiplayerSettings;
                break;

            case SceneState.Hotseat:
                _currentSettings = hotseatSettings;
                break;

            case SceneState.PlayerVSBot:
                _currentSettings = playerVsBotSettings;
                break;

            case SceneState.Undefined:
            default:
                _currentSettings = null;
                Debug.LogWarning("SceneState is Undefined. Game settings not selected");
                break;
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

        string text =
            $"Mode: {_sceneState}\n" +
            $"Turn Time: {_currentSettings.TurnTime}\n" +
            $"Field Size: {_currentSettings.FieldSize}\n" +
            $"Bosses: {_currentSettings.BossCount}\n" +
            $"Party Count: {_currentSettings.PartyCount}\n" +
            $"Boss Difficulty: {_currentSettings.BossDifficulty}\n";

        if (_currentSettings is PlayerVsBotSettings botSettings)
        {
            text += $"Bot Difficulty: {botSettings.BotDifficulty}\n";
        }

        if (_currentSettings is MultiplayerSettings mp)
        {
            text +=
                $"\nRoom Name: {mp.RoomName}\n" +
                $"Your Name: {mp.YourName}\n" +
                $"Players:\n";

            foreach (var player in mp.PlayerList)
            {
                text += $"- {player}\n";
            }
        }

        if (_currentSettings is HotseatSettings hs)
        {
            text +=
                $"\nPlayer 1: {hs.Player1Name}\n" +
                $"Player 2: {hs.Player2Name}\n";
        }

        _gameStateText.text = text;
    }
    
    public enum SceneState
    {
        Undefined,
        Multiplayer,
        Hotseat,
        PlayerVSBot
    }
}