using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class MultiplayerManager : MonoBehaviourPunCallbacks
{
    public static MultiplayerManager Instance { get; private set; }

    public event System.Action OnPlayerDisconnected;
    public event System.Action OnDisconnectedFromServer;

    private bool _quickMatchRequested;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void QuickMatch()
    {
        _quickMatchRequested = true;
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            StartMatchmaking();
        }
    }

    public void LeaveRoomOnly()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("Not in room");
            return;
        }

        Debug.Log("Leaving room only...");
        _quickMatchRequested = false;
        PhotonNetwork.LeaveRoom();
    }
    
    // public void DisconnectFromPhoton()
    // {
    //     if (!PhotonNetwork.IsConnected)
    //     {
    //         Debug.Log("Вже відключені від Photon");
    //         return;
    //     }
    //
    //     Debug.Log("Відключаємося від Photon...");
    //     _quickMatchRequested = false;
    //     
    //     if (PhotonNetwork.InRoom)
    //         PhotonNetwork.LeaveRoom();
    //     
    //     PhotonNetwork.Disconnect();
    // }

    private void StartMatchmaking()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.InLobby)
            PhotonNetwork.JoinLobby();
        else
            PhotonNetwork.JoinRandomRoom();
    }

    #region Photon callbacks

    public override void OnConnectedToMaster()
    {
        if (_quickMatchRequested)
            PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        if (_quickMatchRequested)
            PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
        _quickMatchRequested = false;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.NickName} left the room.");
        OnPlayerDisconnected?.Invoke();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected from Photon: {cause}");
        OnDisconnectedFromServer?.Invoke();
    }

    #endregion

    private void CreateRoom()
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions
        {
            MaxPlayers = 2,
            IsOpen = true,
            IsVisible = true
        });
    }
}