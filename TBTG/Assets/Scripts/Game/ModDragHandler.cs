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

        // спробувати списати очки через event
        bool success = ModAttachHere?.Invoke(price) ?? false;

        if (!success)
        {
            // недостатньо очок — повернути мод додому
            ReturnHome();
            return;
        }

        var modsContainer = card.GetComponent<ModsCardContainer>();
        if (modsContainer != null)
        {
            if (modsContainer.TryAddMod(_modInfo.ModData))
            {
                // успіх — прикріплюємо мод до контейнера
                transform.SetParent(modsContainer.transform, false);
                
                rectTransform.anchorMin =
                    rectTransform.anchorMax =
                        rectTransform.pivot = new Vector2(0.5f, 0.5f);

                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
                return;
            }
        }

        // Якщо контейнера немає або він повний, але ми все одно хочемо прикріпити (або повернути додому, якщо це помилка логіки)
        // В даному випадку, якщо немає контейнера, логічно прикріпити просто до картки або повернути. 
        // Згідно завдання, треба в контейнер. Якщо не вдалося — повертаємо.
        
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