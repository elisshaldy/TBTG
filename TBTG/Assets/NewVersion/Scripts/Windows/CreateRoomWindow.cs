using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class CreateRoomWindow : UIWindow, IMatchmakingCallbacks
{
    [SerializeField] private TMP_InputField _inputFieldRoomName;
    [SerializeField] private Button _createBtn;

    private void Awake()
    {
        _createBtn.onClick.AddListener(OnCreateButtonClicked);
    }

    private void OnDestroy()
    {
        _createBtn.onClick.RemoveListener(OnCreateButtonClicked);
    }

    private void OnCreateButtonClicked()
    {
        string roomName = _inputFieldRoomName.text;
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name is empty!");
            return;
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
        Debug.Log($"Creating room: {roomName}");
    }
    
    public void OnCreatedRoom()
    {
        Debug.Log("Room successfully created!");
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to create room: {message}");
    }
    
    public void OnFriendListUpdate(System.Collections.Generic.List<FriendInfo> friendList) { }
    public void OnJoinedRoom() { }
    public void OnJoinRoomFailed(short returnCode, string message) { }
    public void OnJoinRandomFailed(short returnCode, string message) { }
    public void OnLeftRoom() { }
}