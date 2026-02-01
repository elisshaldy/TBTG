using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCameraController : MonoBehaviour
{
    public static bool BlockCameraControl = false;
    
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
    private float _currentYaw = 0f;
    private float _currentPitch = 45f;

    private void Start()
    {
        if (_cameraRef != null)
        {
            // Optional: Initialize zoom based on current distance
            // _currentZoom = Vector3.Distance(transform.position, _cameraRef.position);
        }
    }
    
    private void LateUpdate()
    {
        if (_cameraRef == null) return;

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
        if (Input.GetMouseButton(0)) 
        {
            _currentYaw += Input.GetAxis("Mouse X") * _rotationSpeed;
            
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