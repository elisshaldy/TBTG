using Photon.Pun;
using UnityEngine;
using System;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance;
    
    public event Action<string, string> OnRoomJoined;
    public event Action OnConnectedToMasterEvent;
    
    public string CurrentRoomName { get; private set; }

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
    
    public void ConnectToPhoton()
    {
        if (Instance != null && Instance != this)
        {
            Instance.ConnectToPhoton();
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Already connected to Photon Master Server");
            OnConnectedToMasterEvent?.Invoke();
            return;
        }

        Debug.Log("Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
        
        PhotonNetwork.NickName = "Player_" + UnityEngine.Random.Range(1000, 9999);
        Debug.Log($"Generated temporary NickName: {PhotonNetwork.NickName}");
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon");
        PhotonNetwork.JoinLobby();
        OnConnectedToMasterEvent?.Invoke();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined lobby");
    }
    
    public override void OnJoinedRoom()
    {
        CurrentRoomName = PhotonNetwork.CurrentRoom.Name;
        Debug.Log($"Joined to room {CurrentRoomName} as {PhotonNetwork.NickName}");
        
        OnRoomJoined?.Invoke(CurrentRoomName, PhotonNetwork.NickName);
    }
}