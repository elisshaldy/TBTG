using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class DisconnectedWindow : UIWindow
{
    [SerializeField] private Button _backToMenuBtn;

    private void Awake()
    {
        if (_backToMenuBtn != null)
        {
            _backToMenuBtn.onClick.AddListener(OnBackToMenuClicked);
        }
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
