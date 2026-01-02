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
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Already connected");
            return;
        }

        Debug.Log("Connect to Photon...");
        PhotonNetwork.ConnectUsingSettings();
        
        PhotonNetwork.NickName = "Player_" + UnityEngine.Random.Range(1000, 9999);
        Debug.Log(PhotonNetwork.NickName);
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected до Photon");
        PhotonNetwork.JoinLobby();
        OnConnectedToMasterEvent?.Invoke();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Enter lobby");
    }
    
    public override void OnJoinedRoom()
    {
        CurrentRoomName = PhotonNetwork.CurrentRoom.Name;
        Debug.Log($"Join to room {CurrentRoomName} as {PhotonNetwork.NickName}");
        
        OnRoomJoined?.Invoke(CurrentRoomName, PhotonNetwork.NickName);
    }
}