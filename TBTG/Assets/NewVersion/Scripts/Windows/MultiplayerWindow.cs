using UnityEngine.UI;
using UnityEngine;

public class MultiplayerWindow : UIWindow
{
    [SerializeField] private Button _quickMatchBtn;
    [SerializeField] private Button _createRoomBtn;
    [SerializeField] private Button _joinRoomBtn;

    private void Start()
    {
        SetButtonsInteractable(false);

        _quickMatchBtn.onClick.AddListener(OnQuickMatchClicked);

        PhotonManager.Instance.OnConnectedToMasterEvent += OnPhotonConnected;
    }

    private void OnDestroy()
    {
        if (PhotonManager.Instance != null)
            PhotonManager.Instance.OnConnectedToMasterEvent -= OnPhotonConnected;
    }

    private void OnPhotonConnected()
    {
        SetButtonsInteractable(true);
    }

    private void OnQuickMatchClicked()
    {
        SetButtonsInteractable(false);
        MultiplayerManager.Instance.QuickMatch();
    }

    private void SetButtonsInteractable(bool state)
    {
        _quickMatchBtn.interactable = state;
        _createRoomBtn.interactable = state;
        _joinRoomBtn.interactable = state;
    }
}