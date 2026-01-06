using UnityEngine;
using UnityEngine.EventSystems;

public class ModDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Transform homeParent;
    private Vector3 homeLocalPosition;
    private Vector3 homeLocalScale;
    private Vector2 homeSizeDelta;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        homeParent = transform.parent;
        homeLocalPosition = rectTransform.localPosition;
        homeLocalScale = rectTransform.localScale;
        homeSizeDelta = rectTransform.sizeDelta;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        
        if (transform.parent == canvas.transform)
        {
            ReturnHome();
        }
    }

    public void AttachToCard(RectTransform card)
    {
        transform.SetParent(card, false);

        rectTransform.anchorMin =
        rectTransform.anchorMax =
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    private void ReturnHome()
    {
        transform.SetParent(homeParent, false);

        rectTransform.localPosition = homeLocalPosition;
        rectTransform.localScale = homeLocalScale;
        rectTransform.sizeDelta = homeSizeDelta;
        rectTransform.localRotation = Quaternion.identity;
    }
}