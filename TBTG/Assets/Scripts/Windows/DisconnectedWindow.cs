using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class DisconnectedWindow : UIWindow
{
    [SerializeField] private Button _backToMenuBtn;

    private void OnEnable()
    {
        // Скидаємо скейлер для всіх карток (оптимізовано)
        CardScaler.ResetAll();
    }

    private void Awake()
    {
        if (_backToMenuBtn != null)
        {
            _backToMenuBtn.onClick.AddListener(OnBackToMenuClicked);
        }

        gameObject.SetActive(false);
    }

    private void OnBackToMenuClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        
        SceneManager.LoadScene("Menu");
    }
}
