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

        // Створюємо подію НАТИСКАННЯ (а не кліку)
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => OnButtonClick());

        // Очищуємо старі події та додаємо нові
        eventTrigger.triggers.Clear();
        eventTrigger.triggers.Add(pointerEnterEntry);
        eventTrigger.triggers.Add(pointerDownEntry);

        // УВАГА: Не додаємо слухача до стандартного onClick,
        // бо він викликається при відпусканні кнопки миші
        // button.onClick.AddListener(OnButtonClick); // ЦЕЙ РЯДОК КОМЕНТУЄМО
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

    // Метод для додавання нових кнопок динамічно
    public void AddButton(Button newButton)
    {
        if (!sceneButtons.Contains(newButton))
        {
            sceneButtons.Add(newButton);
            SetupButtonSounds(newButton);
        }
    }

    // Очищення при знищенні об'єкта
    void OnDestroy()
    {
        foreach (Button button in sceneButtons)
        {
            if (button != null)
            {
                // Оскільки ми не додавали слухача до onClick, не потрібно видаляти
                // Але видаляємо EventTrigger компонент, якщо хочете
                EventTrigger trigger = button.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    trigger.triggers.Clear();
                }
            }
        }
        sceneButtons.Clear();
    }

    // Додатковий метод для оновлення всіх кнопок, якщо змінилася сцена
    public void RefreshAllButtons()
    {
        // Очищуємо старі підписки
        foreach (Button button in sceneButtons)
        {
            if (button != null)
            {
                EventTrigger trigger = button.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    trigger.triggers.Clear();
                }
            }
        }

        sceneButtons.Clear();
        FindAndSetupAllButtons();
    }
}