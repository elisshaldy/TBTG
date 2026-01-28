using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PersistentMusicManager : MonoBehaviour
{
    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float fadeOutDuration = 3f;
    [SerializeField] private float maxVolume = 0.8f;

    [Header("Scene Transition Settings")]
    [SerializeField] private float sceneSwitchFadeDelay = 10f; 
    [SerializeField] private bool stopMusicOnSceneChange = true;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip _cardPickup;
    [SerializeField] private AudioClip _cardHover;
    [SerializeField] private AudioClip _cardPlaced;
    [SerializeField] private AudioClip _cardReturned;
    [SerializeField] private AudioClip _tooltipShow;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private string _currentSceneName;
    private Coroutine _fadeOutCoroutine;
    
    public static PersistentMusicManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        _musicSource = GetComponent<AudioSource>();
        if (_musicSource == null) _musicSource = gameObject.AddComponent<AudioSource>();
        
        _sfxSource = gameObject.AddComponent<AudioSource>();

        SetupAudioSources();
        _currentSceneName = SceneManager.GetActiveScene().name;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void SetupAudioSources()
    {
        _musicSource.clip = backgroundMusic;
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;
        _musicSource.volume = 0f;

        _sfxSource.playOnAwake = false;
        _sfxSource.loop = false;
    }

    void Start()
    {
        StartCoroutine(FadeInMusic());
    }

    public void PlayCardPickup() => PlaySFX(_cardPickup);
    public void PlayCardHover() => PlaySFX(_cardHover);
    public void PlayCardPlaced() => PlaySFX(_cardPlaced);
    public void PlayCardReturned() => PlaySFX(_cardReturned);
    public void PlayTooltipShow() => PlaySFX(_tooltipShow);

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && _sfxSource != null)
        {
            _sfxSource.PlayOneShot(clip);
        }
    }

    IEnumerator FadeInMusic()
    {
        _musicSource.Play();

        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeInDuration;
            _musicSource.volume = Mathf.Lerp(0f, maxVolume, t);
            yield return null;
        }

        _musicSource.volume = maxVolume;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newSceneName = scene.name;

        if (!stopMusicOnSceneChange)
        {
            _currentSceneName = newSceneName;
            return;
        }

        if (_currentSceneName != newSceneName)
        {
            if (_fadeOutCoroutine != null)
            {
                StopCoroutine(_fadeOutCoroutine);
            }

            _fadeOutCoroutine = StartCoroutine(DelayedFadeOut());
            _currentSceneName = newSceneName;
        }
    }

    IEnumerator DelayedFadeOut()
    {
        yield return new WaitForSeconds(sceneSwitchFadeDelay);
        yield return StartCoroutine(FadeOutMusic());
    }

    IEnumerator FadeOutMusic()
    {
        float startVolume = _musicSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;
            _musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        _musicSource.volume = 0f;
        _musicSource.Stop();
    }

    public void StopMusicWithFade(float duration = -1)
    {
        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
        }

        _fadeOutCoroutine = StartCoroutine(FadeOutMusic(duration > 0 ? duration : fadeOutDuration));
    }

    IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = _musicSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            _musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        _musicSource.volume = 0f;
        _musicSource.Stop();
        _fadeOutCoroutine = null;
    }

    public void RestartMusic()
    {
        if (_musicSource.isPlaying)
        {
            StopAllCoroutines();
            _musicSource.Stop();
        }

        _musicSource.volume = 0f;
        _musicSource.Play();
        StartCoroutine(FadeInMusic());
    }

    public void ChangeMusic(AudioClip newMusic, float crossfadeDuration = 2f)
    {
        StartCoroutine(CrossfadeMusic(newMusic, crossfadeDuration));
    }

    IEnumerator CrossfadeMusic(AudioClip newMusic, float duration)
    {
        float halfDuration = duration / 2f;
        float startVolume = _musicSource.volume;

        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            _musicSource.volume = Mathf.Lerp(startVolume, 0f, t / halfDuration);
            yield return null;
        }

        _musicSource.clip = newMusic;
        _musicSource.Play();

        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            _musicSource.volume = Mathf.Lerp(0f, maxVolume, t / halfDuration);
            yield return null;
        }

        _musicSource.volume = maxVolume;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public static void StopMusic()
    {
        if (Instance != null && Instance._musicSource.isPlaying)
        {
            Instance.StopMusicWithFade();
        }
    }

    public static void PlayMusic()
    {
        if (Instance != null && !Instance._musicSource.isPlaying)
        {
            Instance.RestartMusic();
        }
    }

    public static void SetVolume(float volume)
    {
        if (Instance != null)
        {
            Instance.maxVolume = Mathf.Clamp01(volume);
            Instance._musicSource.volume = Instance.maxVolume;
        }
    }
}
