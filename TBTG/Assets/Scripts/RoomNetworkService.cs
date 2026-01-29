using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomNetworkService : MonoBehaviour, IInRoomCallbacks
{
    public event Action<List<Player>> OnPlayerListUpdated;
    public event Action<bool> OnHostStatusChanged;
    public event Action<string> OnRoomNameSet;

    private List<Player> _currentPlayers = new List<Player>();

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void RefreshState()
    {
        UpdateRoomName();
        UpdatePlayerList();
        UpdateHostStatus();
    }

    private void UpdateRoomName()
    {
        if (PhotonManager.Instance != null && !string.IsNullOrEmpty(PhotonManager.Instance.CurrentRoomName))
        {
            OnRoomNameSet?.Invoke(PhotonManager.Instance.CurrentRoomName);
        }
        else if (PhotonNetwork.CurrentRoom != null) 
        {
             OnRoomNameSet?.Invoke(PhotonNetwork.CurrentRoom.Name);
        }
    }

    private void UpdatePlayerList()
    {
        _currentPlayers.Clear();
        if (PhotonNetwork.PlayerList != null)
        {
            _currentPlayers.AddRange(PhotonNetwork.PlayerList);
        }
        OnPlayerListUpdated?.Invoke(_currentPlayers);
    }

    private void UpdateHostStatus()
    {
        OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // Debug.Log("Host started the game");
        PhotonNetwork.LoadLevel("Game");
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        // Debug.Log($"New host: {newMasterClient.NickName}");
        UpdateHostStatus();
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
}
