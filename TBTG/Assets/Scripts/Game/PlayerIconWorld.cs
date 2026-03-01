using UnityEngine;
using UnityEngine.UI;

public class PlayerIconWorld : MonoBehaviour
{
    [SerializeField] private Image _face1;

    private Transform _mainCameraTransform;

    private void Start()
    {
        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
    }

    public bool IsVisible { get; private set; } = true;

    public void SetIcon(Sprite icon)
    {
        if (_face1 != null) _face1.sprite = icon;
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
        if (_face1 != null) _face1.gameObject.SetActive(visible);
    }

    public void Fade(bool visible)
    {
        IsVisible = visible;
        if (_face1 != null)
        {
            float targetAlpha = visible ? 1f : 0f;
            _face1.CrossFadeAlpha(targetAlpha, 0.2f, false);
            if (visible) _face1.gameObject.SetActive(true);
        }
    }

    private void LateUpdate()
    {
        if (_mainCameraTransform == null)
        {
            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
            else return;
        }

        // Calculate the rotation to face the camera (only Y axis to avoid tilting)
        Vector3 cameraEuler = _mainCameraTransform.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(0, cameraEuler.y, 0);

        // Apply rotation only to the icon images
        if (_face1 != null) _face1.transform.rotation = targetRotation;
    }
}