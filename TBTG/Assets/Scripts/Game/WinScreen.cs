using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinScreen : MonoBehaviour
{
    [SerializeField] private Button _mainMenuBtn;

    private void Awake()
    {
        if (_mainMenuBtn != null)
            _mainMenuBtn.onClick.AddListener(() => {
                if (Photon.Pun.PhotonNetwork.InRoom)
                {
                    Photon.Pun.PhotonNetwork.AutomaticallySyncScene = false;
                    Photon.Pun.PhotonNetwork.LeaveRoom();
                }
                SceneManager.LoadScene("Menu");
            });
        
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        PersistentMusicManager.StopMusic(); 
    }
}