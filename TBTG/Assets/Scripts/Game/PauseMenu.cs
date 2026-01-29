using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button _continiueBtn;
    [SerializeField] private Button _settingsBtn;
    [SerializeField] private Button _exitMenuBtn;
    [SerializeField] private Button _exitGameBtn;
    
    [SerializeField] private Button _backBtnSettings;
    
    [SerializeField] private GameObject _menuContent;
    
    [SerializeField] private GameObject _groupBtn;
    [SerializeField] private GameObject _groupSettings;

    private void Start()
    {
        _continiueBtn.onClick.AddListener(ToggleMenu);
        _settingsBtn.onClick.AddListener(OpenSettings);
        _backBtnSettings.onClick.AddListener(CloseSettings);
        _exitMenuBtn.onClick.AddListener(ExitToMenu);
        _exitGameBtn.onClick.AddListener(ExitGame);
        
        if (_menuContent != null)
        {
            _menuContent.SetActive(false);
        }

        CloseSettings();
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
            bool isActive = !_menuContent.activeSelf;
            _menuContent.SetActive(isActive);
            
            if (isActive)
            {
                // Скидаємо скейлер для всіх карток, щоб вони не залишалися збільшеними
                foreach (var scaler in Object.FindObjectsByType<CardScaler>(FindObjectsSortMode.None))
                {
                    scaler.ResetHover();
                }
            }
            else
            {
                CloseSettings();
            }
        }
    }

    private void OpenSettings()
    {
        if (_groupBtn != null) _groupBtn.SetActive(false);
        if (_groupSettings != null) _groupSettings.SetActive(true);
    }

    private void CloseSettings()
    {
        if (_groupBtn != null) _groupBtn.SetActive(true);
        if (_groupSettings != null) _groupSettings.SetActive(false);
    }

    private void ExitToMenu()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        
        SceneManager.LoadScene("Menu");
    }

    private void ExitGame()
    {
        Application.Quit();
    }
}
