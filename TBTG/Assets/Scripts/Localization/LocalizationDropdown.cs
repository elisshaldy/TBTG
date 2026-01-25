using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class LocalizationDropdown : MonoBehaviour
{
    private TMP_Dropdown _dropdown;

    private void Start()
    {
        _dropdown = GetComponent<TMP_Dropdown>();

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.RegisterDropdown(_dropdown);
        }
        else
        {
            // Optional: Retry if Manager initializes slightly later (though typically Manager is DontDestroyOnLoad)
            // Debug.LogWarning("[LocalizationDropdown] LocalizationManager instance not found yet!");
        }
    }
}
