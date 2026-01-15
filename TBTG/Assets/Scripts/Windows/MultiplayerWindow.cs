using TMPro;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

public class MultiplayerWindow : UIWindow
{
    [SerializeField] private TMP_InputField _inputFieldName;
    [SerializeField] private TextMeshProUGUI _playerNameTxt;
    [SerializeField] private Button _enterName;
    
    [SerializeField] private Button _quickMatchBtn;
    [SerializeField] private Button _createRoomBtn;
    [SerializeField] private Button _joinRoomBtn;

    private void Start()
    {
        SetButtonsInteractable(false);
        
        _enterName.onClick.AddListener(OnEnterNameClicked);
        _quickMatchBtn.onClick.AddListener(OnQuickMatchClicked);

        PhotonManager.Instance.OnConnectedToMasterEvent += OnPhotonConnected;
    }
    
    private void OnEnterNameClicked()
    {
        string playerName = _inputFieldName.text.Trim();

        if (string.IsNullOrEmpty(playerName))
            return;

        SetPlayerName(playerName);
    }
    
    private void SetPlayerName(string name)
    {
        PhotonNetwork.NickName = name;

        _playerNameTxt.text = "You current name: " + name;

        Debug.Log($"Player name set to: {PhotonNetwork.NickName}");
        _inputFieldName.text = "";
    }

    private void OnDestroy()
    {
        if (PhotonManager.Instance != null)
            PhotonManager.Instance.OnConnectedToMasterEvent -= OnPhotonConnected;
    }

    private void OnPhotonConnected()
    {
        SetButtonsInteractable(true);
        _playerNameTxt.text = "You current name: " + PhotonNetwork.NickName;
    }

    private void OnQuickMatchClicked()
    {
        SetButtonsInteractable(false);
        MultiplayerManager.Instance.QuickMatch();
    }

    private void SetButtonsInteractable(bool state)
    {
        _inputFieldName.interactable = state;
        _enterName.interactable = state;
        
        _quickMatchBtn.interactable = state;
        _createRoomBtn.interactable = state;
        _joinRoomBtn.interactable = state;
    }
}