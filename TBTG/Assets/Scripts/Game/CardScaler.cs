using UnityEngine;
using UnityEngine.EventSystems;

public class CardScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.5f;
    [SerializeField] private float speed = 15f;

    private Vector3 normalScale;
    private Vector3 targetScale;

    private Transform homeParent;
    private int homeSiblingIndex;
    private Vector3 homeLocalPosition;
    private Quaternion homeLocalRotation;

    private Vector3 returnPosition;
    private Quaternion returnRotation;
    private bool returningHome;

    private Canvas canvas;
    private RectTransform rectTransform;

    private bool isDraggingSelf;
    private bool isHovered;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        normalScale = transform.localScale;
        targetScale = normalScale;

        homeParent = transform.parent;
        homeSiblingIndex = transform.GetSiblingIndex();
        homeLocalPosition = transform.localPosition;
        homeLocalRotation = transform.localRotation;

        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        // Плавний скейл
        transform.localScale =
            Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);

        // Плавне повернення на домашнє місце
        if (returningHome)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, returnPosition, Time.deltaTime * speed);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, returnRotation, Time.deltaTime * speed);

            if (Vector3.Distance(transform.localPosition, returnPosition) < 0.001f &&
                Quaternion.Angle(transform.localRotation, returnRotation) < 0.1f)
            {
                transform.localPosition = returnPosition;
                transform.localRotation = returnRotation;
                returningHome = false;
            }
        }

        if (isHovered && !isDraggingSelf)
        {
            KeepOnScreen();
        }
    }

    private void KeepOnScreen()
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            minX = Mathf.Min(minX, sp.x);
            maxX = Mathf.Max(maxX, sp.x);
            minY = Mathf.Min(minY, sp.y);
            maxY = Mathf.Max(maxY, sp.y);
        }

        Vector2 shift = Vector2.zero;
        if (minX < 0) shift.x = -minX;
        else if (maxX > Screen.width) shift.x = Screen.width - maxX;

        if (minY < 0) shift.y = -minY;
        else if (maxY > Screen.height) shift.y = Screen.height - maxY;

        if (shift.sqrMagnitude > 0.01f)
        {
            if (cam != null)
            {
                RectTransform canvasRect = canvas.transform as RectTransform;
                Vector3 worldPos = transform.position;
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
                screenPos += shift;

                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPos, cam, out Vector3 newWorldPos))
                {
                    transform.position = newWorldPos;
                }
            }
            else
            {
                transform.position += (Vector3)shift;
            }
        }
    }

    public void UpdateHome()
    {
        if (returningHome)
        {
            transform.localPosition = returnPosition;
            transform.localRotation = returnRotation;
            returningHome = false;
        }

        if (isHovered || isDraggingSelf)
            return;

        homeParent = transform.parent;
        homeSiblingIndex = transform.GetSiblingIndex();
        homeLocalPosition = transform.localPosition;
        homeLocalRotation = transform.localRotation;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null || isDraggingSelf || isHovered)
            return;

        UpdateHome();
        isHovered = true;

        targetScale = normalScale * hoverScale;

        // Змінюємо батька на Canvas для hover
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered)
            return;

        isHovered = false;

        targetScale = normalScale;

        // Плавне повернення на домашнє місце
        transform.SetParent(homeParent, true);
        transform.SetSiblingIndex(homeSiblingIndex);

        returnPosition = homeLocalPosition;
        returnRotation = homeLocalRotation;
        returningHome = true;
    }

    public void SetDragging(bool dragging)
    {
        isDraggingSelf = dragging;

        if (dragging)
        {
            isHovered = false;
            targetScale = normalScale;
            transform.localScale = normalScale;
            returningHome = false;
        }
    }
}