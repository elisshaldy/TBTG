using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Transform _mainCameraTransform;

    private void Start()
    {
        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (_mainCameraTransform == null)
        {
            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
            else return;
        }

        // Face the camera, but only on the Y axis if desired, or fully
        // The user said "має такі самі властитовості як і PlayerIconWorld"
        // Let's match PlayerIconWorld's logic (only Y rotation to avoid tilting)
        Vector3 cameraEuler = _mainCameraTransform.eulerAngles;
        transform.rotation = Quaternion.Euler(0, cameraEuler.y, 0);
    }
}
