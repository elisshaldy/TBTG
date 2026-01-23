using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class ButtonSoundManager : MonoBehaviour
{
    [Header("Sound Clips")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    [Header("Audio Settings")]
    [SerializeField] private float hoverVolume = 1f;
    [SerializeField] private float clickVolume = 1f;

    private AudioSource audioSource;
    private List<Button> sceneButtons = new List<Button>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        FindAndSetupAllButtons();
    }

    void FindAndSetupAllButtons()
    {
        // Знаходимо всі кнопки на сцені
        Button[] allButtons = FindObjectsOfType<Button>(true);
        sceneButtons.AddRange(allButtons);

        foreach (Button button in sceneButtons)
        {
            SetupButtonSounds(button);
        }

        Debug.Log($"ButtonSoundManager: Налаштовано {sceneButtons.Count} кнопок на сцені");
    }

    void SetupButtonSounds(Button button)
    {
        // Додаємо EventTrigger якщо його ще немає
        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Створюємо вхідну подію (наведення миші)
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
        pointerEnterEntry.callback.AddListener((data) => OnButtonHover());

        // Створюємо подію натискання
        EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry();
        pointerClickEntry.eventID = EventTriggerType.PointerClick;
        pointerClickEntry.callback.AddListener((data) => OnButtonClick());

        // Очищуємо старі події та додаємо нові
        eventTrigger.triggers.Clear();
        eventTrigger.triggers.Add(pointerEnterEntry);
        eventTrigger.triggers.Add(pointerClickEntry);

        // Також налаштовуємо стандартне натискання кнопки (для подвійного захисту)
        button.onClick.AddListener(OnButtonClick);
    }

    void OnButtonHover()
    {
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    void OnButtonClick()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
        }
    }

    // Метод для додавання нових кнопок динамічно (на випадок, якщо ви створюєте кнопки під час гри)
    public void AddButton(Button newButton)
    {
        if (!sceneButtons.Contains(newButton))
        {
            sceneButtons.Add(newButton);
            SetupButtonSounds(newButton);
        }
    }

    // Очищення списку при знищенні об'єкта
    void OnDestroy()
    {
        foreach (Button button in sceneButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }
        sceneButtons.Clear();
    }
}