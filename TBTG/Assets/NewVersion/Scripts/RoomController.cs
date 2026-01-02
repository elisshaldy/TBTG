using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class RoomUIController : MonoBehaviourPunCallbacks
{
    [SerializeField] private UIWindow _roomWindow;
    [SerializeField] private JoinRoomWindow _joinRoomWindow;

    private void Start()
    {
        PhotonManager.Instance.OnRoomJoined += OnRoomJoined;
        
        if (_joinRoomWindow != null)
            _joinRoomWindow.OnRoomClicked += JoinRoom;
    }

    private void OnDestroy()
    {
        if (PhotonManager.Instance != null)
            PhotonManager.Instance.OnRoomJoined -= OnRoomJoined;
        
        if (_joinRoomWindow != null)
            _joinRoomWindow.OnRoomClicked -= JoinRoom;
    }

    private void OnRoomJoined(string roomName, string playerName)
    {
        WindowManager.Instance.OpenWindow(_roomWindow);
    }
    
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        var rooms = new List<string>();
        foreach (var room in roomList)
        {
            if (!room.RemovedFromList)
                rooms.Add(room.Name);
        }

        _joinRoomWindow?.UpdateRoomList(rooms);
    }
    
    private void JoinRoom(string roomName)
    {
        Debug.Log($"Joining room: {roomName}");
        PhotonNetwork.JoinRoom(roomName);
    }
    
    public override void OnJoinedRoom()
    { 
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
        
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Я хост (MasterClient)");
        }
        else
        {
            Debug.Log("Я клієнт");
        }
        // Тут можна відкривати інше вікно або стартувати гру
    }
}
