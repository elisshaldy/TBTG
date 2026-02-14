using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCameraController : MonoBehaviour
{
    public static bool BlockCameraControl = false;
    public static PlayerCameraController Instance { get; private set; }
    
    [SerializeField] private Transform _cameraRef;

    [Header("Settings")]
    [SerializeField] private float _rotationSpeed = 5.0f;
    [SerializeField] private float _zoomSpeed = 5.0f;
    [SerializeField] private float _minZoom = 5.0f;
    [SerializeField] private float _maxZoom = 20.0f;
    [SerializeField] private float _minPitch = 10.0f;
    [SerializeField] private float _maxPitch = 85.0f;
    [SerializeField] private Vector2 _framingOffset; // Зміщення центру екрана відносно гравця

    private float _currentZoom = 10f;
    private float _currentYaw = 135f;
    private float _currentPitch = 45f;
    private float _targetYaw = 135f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeRotation();
        }
    }

    private void Start()
    {
        if (_cameraRef != null)
        {
            // Optional: Initialize zoom based on current distance
        }
    }

    private void InitializeRotation()
    {
        int localPlayer = 1;
        if (Photon.Pun.PhotonNetwork.InRoom)
            localPlayer = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
        else if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.CurrentSettings != null)
            localPlayer = GameSettingsManager.Instance.CurrentSettings.CurrentPlayerIndex;

        RotateToPlayer(localPlayer, true);
    }

    public void RotateToPlayer(int playerID, bool immediate = false)
    {
        // P1: Top-Left (135), P2: Bottom-Right (-45 or 315)
        _targetYaw = (playerID == 2) ? -45f : 135f;
        
        if (immediate)
        {
            _currentYaw = _targetYaw;
        }

        Debug.Log($"[Camera] Rotating to Player {playerID} (Target Yaw: {_targetYaw})");
    }
    
    private void LateUpdate()
    {
        if (_cameraRef == null) return;

        // Smoothly interpolate to target yaw
        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Time.deltaTime * 3f);

        // Блокуємо керування камерою, якщо вона заблокована ззовні або курсор знаходиться над UI
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        
        if (!BlockCameraControl && !isPointerOverUI)
        {
            HandleZoom();
            HandleRotation();
        }

        UpdateCameraPosition();
    }

    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            _currentZoom -= scrollInput * _zoomSpeed;
            _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
        }
    }

    private void HandleRotation()
    {
        // Rotating while Left Mouse Button is held (Updated as per user diff)
        if (Input.GetMouseButton(1)) 
        {
            float deltaX = Input.GetAxis("Mouse X") * _rotationSpeed;
            _currentYaw += deltaX;
            _targetYaw += deltaX; // Keep target in sync with manual rotation
            
            // Adjust pitch with Mouse Y
            _currentPitch -= Input.GetAxis("Mouse Y") * _rotationSpeed; 
            _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);
        }
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0);
        
        // Розраховуємо точку, на яку зводиться камера (центр екрана)
        // Множимо на _currentZoom, щоб зміщення було пропорційним дистанції (щоб гравець не "тікав" при зумі)
        Vector3 localOffset = new Vector3(_framingOffset.x, _framingOffset.y, 0) * _currentZoom;
        Vector3 focusPoint = _cameraRef.position + rotation * localOffset;

        Vector3 offset = rotation * Vector3.back * _currentZoom;

        transform.position = focusPoint + offset;
        transform.LookAt(focusPoint);
    }
}