using UnityEngine;
using UnityEngine.UI;

public class PlayerIconWorld : MonoBehaviour
{
    [SerializeField] private Image _face1;
    [SerializeField] private Image _face2;

    private Transform _mainCameraTransform;

    private void Start()
    {
        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
    }

    public void SetIcon(Sprite icon)
    {
        if (_face1 != null) _face1.sprite = icon;
        if (_face2 != null) _face2.sprite = icon;
    }

    private void LateUpdate()
    {
        if (_mainCameraTransform == null)
        {
            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
            else return;
        }

        // Calculate the rotation to face the camera
        Quaternion targetRotation = Quaternion.LookRotation(_mainCameraTransform.rotation * Vector3.forward,
                         _mainCameraTransform.rotation * Vector3.up);

        // Apply rotation only to the icon images
        if (_face1 != null) _face1.transform.rotation = targetRotation;
        if (_face2 != null) _face2.transform.rotation = targetRotation;
    }
}