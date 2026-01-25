using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PersistentMusicManager : MonoBehaviour
{
    [Header("Налаштування музики")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float fadeOutDuration = 3f;
    [SerializeField] private float maxVolume = 0.8f;

    [Header("Перехід між сценами")]
    [SerializeField] private float sceneSwitchFadeDelay = 10f; // Затримка перед затуханням при зміні сцени
    [SerializeField] private bool stopMusicOnSceneChange = true; // Чи зупиняти музику при зміні сцени

    private AudioSource audioSource;
    private string currentSceneName;
    private Coroutine fadeOutCoroutine;
    private static PersistentMusicManager instance;

    void Awake()
    {
        // Реалізація Singleton патерну
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Initialize()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetupAudioSource();
        currentSceneName = SceneManager.GetActiveScene().name;

        // Підписуємось на події зміни сцени
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void SetupAudioSource()
    {
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    void Start()
    {
        // Починаємо музику з плавним наростанням
        StartCoroutine(FadeInMusic());
    }

    IEnumerator FadeInMusic()
    {
        audioSource.Play();

        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeInDuration;
            audioSource.volume = Mathf.Lerp(0f, maxVolume, t);
            yield return null;
        }

        audioSource.volume = maxVolume;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newSceneName = scene.name;

        // Якщо це перша сцена або ми не хочемо зупиняти музику
        if (!stopMusicOnSceneChange)
        {
            currentSceneName = newSceneName;
            return;
        }

        // Якщо сцена змінилась
        if (currentSceneName != newSceneName)
        {
            // Скасовуємо попередній fade out якщо він був
            if (fadeOutCoroutine != null)
            {
                StopCoroutine(fadeOutCoroutine);
            }

            // Запускаємо затримку перед затуханням
            fadeOutCoroutine = StartCoroutine(DelayedFadeOut());

            currentSceneName = newSceneName;
        }
    }

    void OnSceneUnloaded(Scene scene)
    {
        // Додаткові дії при вивантаженні сцени
    }

    IEnumerator DelayedFadeOut()
    {
        // Чекаємо заданий час
        yield return new WaitForSeconds(sceneSwitchFadeDelay);

        // Плавно затухаємо
        yield return StartCoroutine(FadeOutMusic());
    }

    IEnumerator FadeOutMusic()
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }

    // Публічні методи для ручного керування

    public void StopMusicWithFade(float duration = -1)
    {
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
        }

        fadeOutCoroutine = StartCoroutine(FadeOutMusic(duration > 0 ? duration : fadeOutDuration));
    }

    IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        fadeOutCoroutine = null;
    }

    public void RestartMusic()
    {
        if (audioSource.isPlaying)
        {
            StopAllCoroutines();
            audioSource.Stop();
        }

        audioSource.volume = 0f;
        audioSource.Play();
        StartCoroutine(FadeInMusic());
    }

    public void ChangeMusic(AudioClip newMusic, float crossfadeDuration = 2f)
    {
        StartCoroutine(CrossfadeMusic(newMusic, crossfadeDuration));
    }

    IEnumerator CrossfadeMusic(AudioClip newMusic, float duration)
    {
        float halfDuration = duration / 2f;
        float startVolume = audioSource.volume;

        // Затухаємо поточну музику
        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / halfDuration);
            yield return null;
        }

        // Змінюємо музику
        audioSource.clip = newMusic;
        audioSource.Play();

        // Наростає нова музика
        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, maxVolume, t / halfDuration);
            yield return null;
        }

        audioSource.volume = maxVolume;
    }

    void OnDestroy()
    {
        // Відписуємось від подій при знищенні
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }

    // Статичні методи для зручного доступу з будь-якого місця
    public static void StopMusic()
    {
        if (instance != null && instance.audioSource.isPlaying)
        {
            instance.StopMusicWithFade();
        }
    }

    public static void PlayMusic()
    {
        if (instance != null && !instance.audioSource.isPlaying)
        {
            instance.RestartMusic();
        }
    }

    public static void SetVolume(float volume)
    {
        if (instance != null)
        {
            instance.maxVolume = Mathf.Clamp01(volume);
            instance.audioSource.volume = instance.maxVolume;
        }
    }
}