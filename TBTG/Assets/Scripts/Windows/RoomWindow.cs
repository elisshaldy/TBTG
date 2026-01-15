using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using Photon.Realtime;
using Photon.Pun;

public class RoomWindow : UIWindow
{
    [SerializeField] private SceneState _sceneState;
    [SerializeField] private WindowManager _windowManager;
    [Space(20)]
    [SerializeField] private RoomNetworkService roomNetworkService;
    [SerializeField] private RoomParametersUI _roomParameters;
    [SerializeField] private Button _startGameButton;
    [SerializeField] private GameObject _roomUIPlayerName;   
    [SerializeField] private Transform _playerContainerUI;   

    [SerializeField] private TextMeshProUGUI _roomName;

    private List<GameObject> _spawnedPlayers = new List<GameObject>();

    private void OnEnable()
    {
        if (_windowManager != null)
        {
            _windowManager.OnSceneStateSelected += OnSceneStateSelected;
        }
        
        if (roomNetworkService != null)
        {
            roomNetworkService.OnPlayerListUpdated += RefreshPlayerList;
            roomNetworkService.OnHostStatusChanged += UpdateHostUI;
            roomNetworkService.OnRoomNameSet += UpdateRoomNameDisplay;
            
            roomNetworkService.RefreshState();
        }

        if (_roomParameters != null)
        {
            _roomParameters.OnParametersChanged += UpdateStartButtonInteractable;
        }
    }
    
    private void OnSceneStateSelected(SceneState state)
    {
        _sceneState = state;
        Debug.Log($"[RoomWindow] SceneState updated to: {_sceneState}");

        switch (_sceneState)
        {
            case SceneState.Multiplayer:
                _roomParameters.ShowMultiplayerUI();
                break;

            case SceneState.Hotseat:
                _roomName.text = "Hotseat";
                _roomParameters.ShowHotseatUI();
                break;
            
            case SceneState.PlayerVSBot:
                _roomName.text = "PlayerVSBot";
                _roomParameters.ShowPlayerVSBotUI();
                break;
        }
        
        UpdateHostUI(roomNetworkService != null && PhotonNetwork.IsMasterClient);
        UpdateStartButtonInteractable();
    }
    
    private void RefreshPlayerList(List<Player> players)
    {
        foreach (var obj in _spawnedPlayers)
            Destroy(obj);
        _spawnedPlayers.Clear();
        
        foreach (Player p in players)
        {
            GameObject go = Instantiate(_roomUIPlayerName, _playerContainerUI);

            TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = p.NickName;

            _spawnedPlayers.Add(go);
        }

        UpdateStartButtonInteractable();
    }

    private void UpdateHostUI(bool isHost)
    {
        if (_roomParameters != null)
        {
            _roomParameters.SetInteractable(isHost, _sceneState);
        }
    }

    private void UpdateRoomNameDisplay(string name)
    {
        _roomName.text = $"Room: {name}";
    }

    private void OnDisable()
    {
        if (_windowManager != null)
        {
            _windowManager.OnSceneStateSelected -= OnSceneStateSelected;
        }
        
        if (roomNetworkService != null)
        {
            roomNetworkService.OnPlayerListUpdated -= RefreshPlayerList;
            roomNetworkService.OnHostStatusChanged -= UpdateHostUI;
            roomNetworkService.OnRoomNameSet -= UpdateRoomNameDisplay;
        }

        if (_roomParameters != null)
        {
            _roomParameters.OnParametersChanged -= UpdateStartButtonInteractable;
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        if (roomNetworkService != null)
        {
             roomNetworkService.RefreshState();
        }
        
        OnSceneStateSelected(_windowManager.CurrentSceneState);
        UpdateStartButtonInteractable();
    }

    private void UpdateStartButtonInteractable()
    {
        if (_startGameButton == null) return;

        bool isHost = (_sceneState == SceneState.Multiplayer) ? PhotonNetwork.IsMasterClient : true;
        
        if (!isHost)
        {
            _startGameButton.interactable = false;
            return;
        }

        bool canStart = true;

        switch (_sceneState)
        {
            case SceneState.Multiplayer:
                canStart = PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= 1; // TEMP, DEBUG
                break;
            case SceneState.Hotseat:
                canStart = _roomParameters != null && _roomParameters.AreHotseatNamesValid();
                break;
            case SceneState.PlayerVSBot:
                canStart = true; 
                break;
        }

        _startGameButton.interactable = canStart;
    }
    
    public void OnStartGameClicked()
    {
        GameSettings settings = _roomParameters.GetPopulatedSettings(_sceneState);
        
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.CurrentMode = _sceneState;
            GameSettingsManager.Instance.CurrentSettings = settings;
        }

        if (_sceneState == SceneState.Multiplayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                var props = new ExitGames.Client.Photon.Hashtable();
                props.Add("TurnTime", settings.TurnTime);
                props.Add("FieldSize", settings.FieldSize);
                props.Add("PartyCount", settings.PartyCount);
                props.Add("BossCount", settings.BossCount);
                props.Add("BossDifficulty", (int)settings.BossDifficulty);
            
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                
                if (roomNetworkService != null)
                {
                    roomNetworkService.StartGame();
                }
            }
        }
        else
        {
            // ЛОКАЛЬНИЙ СТАРТ (Hotseat / VS Bot)
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
    }
}
