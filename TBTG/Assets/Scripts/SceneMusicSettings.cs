using UnityEngine;

public class SceneMusicSettings : MonoBehaviour
{
    public enum SceneMusicBehavior
    {
        ContinueMusic,      // Музика продовжує грати
        FadeOutAndStop,     // Музика затухає і зупиняється
        ChangeMusic,        // Змінити музику на іншу
        NoMusic             // Без музики
    }

    [Header("Поведінка музики у цій сцені")]
    [SerializeField] private SceneMusicBehavior musicBehavior = SceneMusicBehavior.ContinueMusic;

    [Header("Якщо зміна музики")]
    [SerializeField] private AudioClip sceneSpecificMusic;
    [SerializeField] private float fadeDelay = 0f;

    [Header("Якщо зупинка")]
    [SerializeField] private float stopDelay = 10f; // Через скільки секунд зупинити

    void Start()
    {
        PersistentMusicManager musicManager = FindObjectOfType<PersistentMusicManager>();

        if (musicManager == null) return;

        switch (musicBehavior)
        {
            case SceneMusicBehavior.ContinueMusic:
                // Нічого не робимо, музика грає далі
                break;

            case SceneMusicBehavior.FadeOutAndStop:
                musicManager.StopMusicWithFade(stopDelay);
                break;

            case SceneMusicBehavior.ChangeMusic:
                if (sceneSpecificMusic != null)
                {
                    StartCoroutine(DelayedMusicChange());
                }
                break;

            case SceneMusicBehavior.NoMusic:
                musicManager.StopMusicWithFade(1f);
                break;
        }
    }

    System.Collections.IEnumerator DelayedMusicChange()
    {
        yield return new WaitForSeconds(fadeDelay);

        PersistentMusicManager musicManager = FindObjectOfType<PersistentMusicManager>();
        if (musicManager != null)
        {
            musicManager.ChangeMusic(sceneSpecificMusic, 2f);
        }
    }
}