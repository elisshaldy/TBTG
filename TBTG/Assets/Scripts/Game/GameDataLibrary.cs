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

    // Метод для рандомного вибору карт
    public List<CharacterData> GetRandomCharacters(int count)
    {
        return AllCharacters.OrderBy(x => System.Guid.NewGuid()).Take(count).ToList();
    }

    // Метод для рандомного вибору модів (тут вже без Critical)
    public List<ModData> GetRandomMods(int count)
    {
        return AllMods.OrderBy(x => System.Guid.NewGuid()).Take(count).ToList();
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Library")]
    public void Refresh()
    {
        // Автоматично шукаємо всі CharacterData в папці Assets/Configs
        AllCharacters = FindAssetsByType<CharacterData>("Assets/Configs/Character Data");
        
        // Шукаємо лише Active та Passive моди
        AllMods = new List<ModData>();
        AllMods.AddRange(FindAssetsByType<ModData>("Assets/Configs/Mods Active"));
        AllMods.AddRange(FindAssetsByType<ModData>("Assets/Configs/Mods Passive"));
        // Critical моди не додаємо

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"Library updated! Found {AllCharacters.Count} characters and {AllMods.Count} mods.");
    }

    private List<T> FindAssetsByType<T>(string folderPath) where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
        return guids.Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
    }
#endif
}