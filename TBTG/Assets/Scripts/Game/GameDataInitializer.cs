using UnityEngine;
using System.Collections.Generic;

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

        // ===== CHARACTERS =====
        List<CharacterData> randomChars = _library.GetRandomCharacters(_cardsInstances.Length);

        if (randomChars.Count < _cardsInstances.Length)
            Debug.LogWarning($"Not enough characters! Need {_cardsInstances.Length}, got {randomChars.Count}");

        // 1️⃣ Спершу присвоюємо дані всім CardInfo
        for (int i = 0; i < _cardsInstances.Length; i++)
        {
            if (_cardsInstances[i] == null) continue;

            if (i < randomChars.Count)
                _cardsInstances[i].CharData = randomChars[i];
        }

        // 2️⃣ Тільки після цього ініціалізуємо UI
        foreach (var card in _cardsInstances)
        {
            if (card == null || card.CharData == null) continue;
            card.Initialize();
        }

        // ===== MODIFIERS =====
        List<ModData> randomMods = _library.GetRandomMods(_modsInstances.Length);

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
}