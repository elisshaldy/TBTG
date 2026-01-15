using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class JoinRoomWindow : UIWindow
{
    public Action<string> OnRoomClicked;
    
    [SerializeField] private LayoutGroup _container;
    [SerializeField] private GameObject _roomPrefabUI;

    private Dictionary<string, GameObject> _roomItems = new Dictionary<string, GameObject>();
    
    public void UpdateRoomList(List<string> roomNames)
    {
        var keysToRemove = new List<string>();
        foreach (var key in _roomItems.Keys)
        {
            if (!roomNames.Contains(key))
            {
                Destroy(_roomItems[key]);
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
            _roomItems.Remove(key);
        
        foreach (var roomName in roomNames)
        {
            if (!_roomItems.ContainsKey(roomName))
                AddRoomItem(roomName);
        }
    }

    private void AddRoomItem(string roomName)
    {
        GameObject newRoom = Instantiate(_roomPrefabUI, _container.transform, false);

        var roomText = newRoom.GetComponentInChildren<TextMeshProUGUI>();
        if (roomText != null) roomText.text = roomName;

        var button = newRoom.GetComponentInChildren<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnRoomClicked?.Invoke(roomName));
        }

        _roomItems.Add(roomName, newRoom);
        LayoutRebuilder.MarkLayoutForRebuild(_container.GetComponent<RectTransform>());
    }
}