using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button _continiueBtn;
    [SerializeField] private Button _settingsBtn;
    [SerializeField] private Button _exitMenuBtn;
    [SerializeField] private Button _exitGameBtn;
    
    [SerializeField] private GameObject _menuContent;

    private void Start()
    {
        _continiueBtn.onClick.AddListener(ToggleMenu);
        
        if (_menuContent != null)
        {
            _menuContent.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (_menuContent != null)
        {
            bool isActive = _menuContent.activeSelf;
            _menuContent.SetActive(!isActive);
        }
    }
}