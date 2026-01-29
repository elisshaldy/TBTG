using UnityEngine;

public class GameNetworkListener : MonoBehaviour
{
    [SerializeField] private DisconnectedWindow _disconnectedWindow;

    private void OnEnable()
    {
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.OnPlayerDisconnected += ShowDisconnectedWindow;
            MultiplayerManager.Instance.OnDisconnectedFromServer += ShowDisconnectedWindow;
        }
    }

    private void OnDisable()
    {
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.OnPlayerDisconnected -= ShowDisconnectedWindow;
            MultiplayerManager.Instance.OnDisconnectedFromServer -= ShowDisconnectedWindow;
        }
    }

    private void ShowDisconnectedWindow()
    {
        if (_disconnectedWindow != null)
        {
            _disconnectedWindow.OnShow();
        }
        else
        {
            Debug.LogWarning("DisconnectedWindow is not assigned in GameNetworkListener!");
        }
    }
}
