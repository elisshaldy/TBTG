using TMPro;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

public class MultiplayerWindow : UIWindow
{
    [SerializeField] private TMP_InputField _inputFieldName;
    [SerializeField] private LocalizationLabel _playerNameTxt;
    [SerializeField] private Button _enterName;
    
    [SerializeField] private Button _quickMatchBtn;
    [SerializeField] private Button _createRoomBtn;
    [SerializeField] private Button _joinRoomBtn;

    private void OnEnable()
    {
        SetButtonsInteractable(false);
        
        if (PhotonManager.Instance != null)
            PhotonManager.Instance.OnConnectedToMasterEvent += OnPhotonConnected;

        // If we are already connected, manually trigger the UI update
        if (PhotonNetwork.IsConnectedAndReady)
        {
            OnPhotonConnected();
        }
    }

    private void OnDisable()
    {
        if (PhotonManager.Instance != null)
            PhotonManager.Instance.OnConnectedToMasterEvent -= OnPhotonConnected;
    }

    private void Start()
    {
        _enterName.onClick.AddListener(OnEnterNameClicked);
        _quickMatchBtn.onClick.AddListener(OnQuickMatchClicked);
    }

    /// <summary>
    /// Use this method for the "Multiplayer" button in the Main Menu to avoid broken references
    /// if PhotonManager gets destroyed/respawned.
    /// </summary>
    public void ConnectToPhotonSafe()
    {
        PhotonManager.Instance.ConnectToPhoton();
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

        _playerNameTxt.SetKey("current_name_player_txt");
        _playerNameTxt.SetSuffix(": " + name);

        Debug.Log($"Player name set to: {PhotonNetwork.NickName}");
        _inputFieldName.text = "";
    }

    private void OnPhotonConnected()
    {
        SetButtonsInteractable(true);
        _playerNameTxt.SetKey("current_name_player_txt");
        _playerNameTxt.SetSuffix(": " + PhotonNetwork.NickName);
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