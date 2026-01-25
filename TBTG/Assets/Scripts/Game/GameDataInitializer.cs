using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class GameDataInitializer : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private GameDataLibrary _library;

    [Header("Scene Instances")]
    [SerializeField] private CardInfo[] _cardsInstances = new CardInfo[10];
    [SerializeField] private ModInfo[] _modsInstances = new ModInfo[35];

    private void Start()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        if (_library == null)
        {
            Debug.LogError("GameDataLibrary is NULL");
            return;
        }

        // 1. Отримуємо налаштування та індекс поточного гравця
        SceneState mode = SceneState.Undefined;
        GameSettings settings = null;
        if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.CurrentSettings != null)
        {
            settings = GameSettingsManager.Instance.CurrentSettings;
            mode = GameSettingsManager.Instance.CurrentMode;
        }

        int playerIndex = GetPlayerIndex(mode);
        Debug.Log($"[Initializer] Initializing for player {playerIndex}");

        // 2. Отримуємо ПЕРЕМІШАНІ індекси персонажів саме для цього гравця
        int[] myIndices = (settings != null) ? settings.GetIndicesForPlayer(playerIndex) : null;
        List<CharacterData> myChars = new List<CharacterData>();

        if (myIndices != null)
        {
            foreach (int idx in myIndices)
            {
                if (idx < _library.AllCharacters.Count)
                    myChars.Add(_library.AllCharacters[idx]);
            }
        }
        else
        {
            // Фоллбек, якщо налаштування чомусь не прийшли
            myChars = _library.GetRandomCharacters(_cardsInstances.Length);
        }

        // ===== CHARACTERS =====
        if (myChars.Count < _cardsInstances.Length)
            Debug.LogWarning($"Not enough characters! Need {_cardsInstances.Length}, got {myChars.Count}");

        // 3. Присвоюємо дані
        for (int i = 0; i < _cardsInstances.Length; i++)
        {
            if (_cardsInstances[i] == null) continue;

            if (i < myChars.Count)
                _cardsInstances[i].CharData = myChars[i];
        }

        // 4. Ініціалізуємо UI
        foreach (var card in _cardsInstances)
        {
            if (card == null || card.CharData == null) continue;
            card.Initialize();
        }

        // ===== MODIFIERS =====
        List<ModData> randomMods = _library.GetModsByBalanceRules();

        if (randomMods.Count < _modsInstances.Length)
            Debug.LogWarning($"Not enough mods! Need {_modsInstances.Length}, got {randomMods.Count}");

        // 1️⃣ Присвоюємо ModData
        for (int i = 0; i < _modsInstances.Length; i++)
        {
            if (_modsInstances[i] == null) continue;

            if (i < randomMods.Count)
                _modsInstances[i].ModData = randomMods[i];
        }

        // 2️⃣ Ініціалізуємо UI
        foreach (var mod in _modsInstances)    
        {
            if (mod == null || mod.ModData == null) continue;
            mod.Initialize();
        }

        Debug.Log("Game Data Initialized SUCCESSFULLY");
    }

    private int GetPlayerIndex(SceneState mode)
    {
        switch (mode)
        {
            case SceneState.Multiplayer:
                if (PhotonNetwork.InRoom)
                {
                    // Many developers use ActorNumber - 1 as a 0-indexed player ID
                    return PhotonNetwork.LocalPlayer.ActorNumber - 1;
                }
                return 0;

            case SceneState.Hotseat:
                // If it's hotseat, we might need to track current player turn.
                // For now, let's assume we logic handles sequential initialization if needed.
                // Or if it's strictly 2 players on same screen, index 0 is used.
                return 0; 

            case SceneState.PlayerVSBot:
                return 0; // Player is always 0, Bot is 1 (if bot needs cards)

            default:
                return 0;
        }
    }
}