using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class ModDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public event Func<int, bool> ModAttachHere;
    public event Action<int> ModDetachHere;
    
    [SerializeField] private ModInfo _modInfo;
    
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
        var parentContainer = GetComponentInParent<ModsCardContainer>();
        if (parentContainer != null)
        {
            parentContainer.RemoveMod(_modInfo.ModData);
            ModDetachHere?.Invoke(_modInfo.ModData.Price);
        }

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
        int price = _modInfo.ModData.Price;

        var modsContainer = card.GetComponent<ModsCardContainer>();
        if (modsContainer == null || !modsContainer.CanAddMod(_modInfo.ModData))
        {
            ReturnHome();
            return;
        }

        bool success = ModAttachHere?.Invoke(price) ?? false;

        if (!success)
        {
            ReturnHome();
            return;
        }

        // Беремо трансформ конкретного слота
        Transform slotTransform = modsContainer.AddMod(_modInfo.ModData);
        
        // Прикріплюємо до слота
        transform.SetParent(slotTransform, false);
        
        rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        // Вимикаємо тіло модифікатора
        gameObject.SetActive(false);
    }

    public void DetachFromCard()
    {
        var parentContainer = GetComponentInParent<ModsCardContainer>();
        if (parentContainer != null)
        {
            parentContainer.RemoveMod(_modInfo.ModData);
            ModDetachHere?.Invoke(_modInfo.ModData.Price);
        }
        
        gameObject.SetActive(true);
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        ReturnHome();
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