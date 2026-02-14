using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GameDataLibrary", menuName = "Game Data/Library")]
public class GameDataLibrary : ScriptableObject
{
    public List<CharacterData> AllCharacters;
    public List<ModData> AllMods;

    public List<MovementCard> AllMovementCards;

    // Перемішує всі доступні карти і повертає їх індекси
    public int[] GetShuffledIndices()
    {
        return Enumerable.Range(0, AllCharacters.Count)
            .OrderBy(x => System.Guid.NewGuid())
            .ToArray();
    }

    public int[] GetShuffledMovementIndices()
    {
        return Enumerable.Range(0, AllMovementCards.Count)
            .OrderBy(x => System.Guid.NewGuid())
            .ToArray();
    }

    public List<CharacterData> GetRandomCharacters(int count)
    {
        return AllCharacters.OrderBy(x => System.Guid.NewGuid()).Take(count).ToList();
    }

    public List<MovementCard> GetRandomMovementCards(int count)
    {
        return AllMovementCards.OrderBy(x => System.Guid.NewGuid()).Take(count).ToList();
    }

    public List<MovementCard> GetMovementCardsFromIndices(int[] indices)
    {
        List<MovementCard> result = new List<MovementCard>();
        if (indices == null) return result;
        foreach (int idx in indices)
        {
            if (idx >= 0 && idx < AllMovementCards.Count)
                result.Add(AllMovementCards[idx]);
        }
        return result;
    }

    // Метод для рандомного вибору модів (тут вже без Critical)
    public List<ModData> GetRandomMods(int count)
    {
        return AllMods.OrderBy(x => System.Guid.NewGuid()).Take(count).ToList();
    }

    public List<ModData> GetModsByBalanceRules()
    {
        List<ModData> result = new List<ModData>();

        // Rule: 5 points -> 3 mods: 1 Passive, 2 Active
        result.AddRange(GetRandomByCriteria(5, ModType.Passive, 1));
        result.AddRange(GetRandomByCriteria(5, ModType.Active, 2));

        // Rule: 4 points -> 5 mods: 1-2 Passive, balance Active
        int p4 = Random.Range(1, 3);
        result.AddRange(GetRandomByCriteria(4, ModType.Passive, p4));
        result.AddRange(GetRandomByCriteria(4, ModType.Active, 5 - p4));

        // Rule: 3 points -> 7 mods: 2-3 Passive, balance Active
        int p3 = Random.Range(2, 4);
        result.AddRange(GetRandomByCriteria(3, ModType.Passive, p3));
        result.AddRange(GetRandomByCriteria(3, ModType.Active, 7 - p3));

        // Rule: 2 points -> 9 mods: 3-4 Passive, balance Active
        int p2 = Random.Range(3, 5);
        result.AddRange(GetRandomByCriteria(2, ModType.Passive, p2));
        result.AddRange(GetRandomByCriteria(2, ModType.Active, 9 - p2));

        // Rule: 1 points -> 11 mods: 4-5 Passive, balance Active
        int p1 = Random.Range(4, 6);
        result.AddRange(GetRandomByCriteria(1, ModType.Passive, p1));
        result.AddRange(GetRandomByCriteria(1, ModType.Active, 11 - p1));

        // Shuffle the final list of 35
        return result.OrderBy(x => System.Guid.NewGuid()).ToList();
    }

    private List<ModData> GetRandomByCriteria(int price, ModType type, int count)
    {
        var pool = AllMods.Where(m => m.Price == price && m.ModType == type).ToList();
        
        if (pool.Count < count)
        {
            Debug.LogWarning($"[Library] Not enough mods of Price {price} and Type {type}! Target: {count}, Available: {pool.Count}");
            return pool; // Return what we have
        }

        return pool.OrderBy(x => System.Guid.NewGuid()).Take(count).ToList();
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Library")]
    public void Refresh()
    {
        // Автоматично шукаємо всі CharacterData в папці Assets/Configs
        AllCharacters = FindAssetsByType<CharacterData>("Assets/Configs/Character Data");
        
        // Шукаємо Movement cards
        AllMovementCards = FindAssetsByType<MovementCard>("Assets/Configs/Movement Cards");

        // Шукаємо лише Active та Passive моди
        AllMods = new List<ModData>();
        AllMods.AddRange(FindAssetsByType<ModData>("Assets/Configs/Mods Active"));
        AllMods.AddRange(FindAssetsByType<ModData>("Assets/Configs/Mods Passive"));
        // Critical моди не додаємо

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"Library updated! Found {AllCharacters.Count} characters, {AllMovementCards.Count} movement cards, and {AllMods.Count} mods.");
    }

    private List<T> FindAssetsByType<T>(string folderPath) where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
        return guids.Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
    }
#endif
}