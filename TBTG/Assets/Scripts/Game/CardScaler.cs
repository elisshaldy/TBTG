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
    private Canvas canvas;

    private bool isDraggingSelf;
    private bool isHovered;

    private void Awake()
    {
        normalScale = transform.localScale;
        targetScale = normalScale;

        homeParent = transform.parent;
        homeSiblingIndex = transform.GetSiblingIndex();

        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        transform.localScale =
            Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }
    
    public void UpdateHome()
    {
        homeParent = transform.parent;
        homeSiblingIndex = transform.GetSiblingIndex();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
            return;

        if (isDraggingSelf || isHovered)
            return;

        isHovered = true;

        targetScale = normalScale * hoverScale;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered)
            return;

        isHovered = false;

        targetScale = normalScale;

        transform.SetParent(homeParent, true);
        transform.SetSiblingIndex(homeSiblingIndex);
    }
    
    public void SetDragging(bool dragging)
    {
        isDraggingSelf = dragging;

        if (dragging)
        {
            isHovered = false;
            targetScale = normalScale;
            transform.localScale = normalScale;
        }
    }
}