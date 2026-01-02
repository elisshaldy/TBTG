using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class RoomWindow : UIWindow, IInRoomCallbacks
{
    [SerializeField] private TMP_InputField _turnTime; 
    [SerializeField] private TMP_Dropdown _fieldSize;
    [SerializeField] private Button _startGame; 
    [Space(20)]
    [SerializeField] private GameObject _roomUIPlayerName;   
    [SerializeField] private Transform _playerContainerUI;   

    [SerializeField] private TextMeshProUGUI _roomName;

    private List<GameObject> _spawnedPlayers = new List<GameObject>();

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
        RefreshPlayerList();
        UpdateHostUI();
    }
    
    private void UpdateHostUI()
    {
        bool isHost = PhotonNetwork.IsMasterClient;

        _turnTime.interactable = isHost;
        _fieldSize.interactable = isHost;
        _startGame.interactable = isHost;
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public override void OnShow()
    {
        base.OnShow();
        UpdateRoomName();
        RefreshPlayerList();
        UpdateHostUI();
    }

    private void UpdateRoomName()
    {
        if (PhotonManager.Instance != null && !string.IsNullOrEmpty(PhotonManager.Instance.CurrentRoomName))
        {
            _roomName.text = $"Room: {PhotonManager.Instance.CurrentRoomName}";
        }
    }

    public void RefreshPlayerList()
    {
        foreach (var obj in _spawnedPlayers)
            Destroy(obj);
        _spawnedPlayers.Clear();
        
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            GameObject go = Instantiate(_roomUIPlayerName, _playerContainerUI);

            TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = p.NickName;

            _spawnedPlayers.Add(go);
        }
    }
    
    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshPlayerList();
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayerList();
    }
    
    public void OnStartGameClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log("Host started the game");

        PhotonNetwork.LoadLevel("Game");
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"New host: {newMasterClient.NickName}");
        UpdateHostUI();
    }
    
    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
}