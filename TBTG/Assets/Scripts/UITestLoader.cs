// UITestLoader.cs
using UnityEngine;

// Цей скрипт призначений лише для перевірки, чи коректно працює візуалізація картки.
public class UITestLoader : MonoBehaviour
{
    [Tooltip("Посилання на скрипт CharacterCardUI на сцені")]
    public CharacterCardUI CardUIComponent;

    [Tooltip("Тестовий ассет CharacterData, який ми хочемо відобразити")]
    public CharacterData TestData;

    void Start()
    {
        // Перевірка, чи всі посилання призначені
        if (CardUIComponent == null)
        {
            Debug.LogError("CardUIComponent не призначено! Призначте CharacterCard_Panel у Інспекторі.");
            return;
        }

        if (TestData == null)
        {
            Debug.LogError("TestData не призначено! Призначте Test_Assassin_Data у Інспекторі.");
            return;
        }

        // !!! ОСНОВНИЙ ВИКЛИК ТЕСТУВАННЯ !!!
        CardUIComponent.DisplayCharacter(TestData);

        Debug.Log("CharacterCardUI successfully displayed data from TestData asset.");
    }
}