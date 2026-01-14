using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public CardSlot CurrentSlot { get; set; }
    public CardSlot LastSlot { get; private set; }

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private GameSceneState gameSceneState;
    private CardDeckController cardDeckController;

    private Transform homeParent;

    // UI parameters to restore after drag
    private Vector2 originalSize;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;
    private Quaternion originalLocalRotation;

    private CardScaler scaler;
    private bool _canDrag = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        scaler = GetComponent<CardScaler>();
        
        homeParent = transform.parent;

        originalSize = rectTransform.sizeDelta;
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;
        originalPivot = rectTransform.pivot;
        originalLocalPosition = rectTransform.localPosition;
        originalLocalScale = rectTransform.localScale;
        originalLocalRotation = rectTransform.localRotation;
    }

    private void Start()
    {
        if (gameSceneState == null)
            gameSceneState = FindObjectOfType<GameSceneState>(); // OPTIMIZE
        if (cardDeckController == null)
            cardDeckController = FindObjectOfType<CardDeckController>(); // OPTIMIZE
    }
    
    public void SetRaycastTarget(bool value)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = value;
        
        var image = GetComponent<Image>(); // OPTIMIZE
        if (image != null) image.raycastTarget = value;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        _canDrag = false;

        // Fallback if Start didn't run or object created later
        if (gameSceneState == null)
            gameSceneState = FindObjectOfType<GameSceneState>();

        if (gameSceneState != null && gameSceneState.CurrentStep != GameSetupStep.Cards)
        {
            eventData.pointerDrag = null; // Cancel drag to allow hover
            return;
        }

        _canDrag = true;

        LastSlot = CurrentSlot;
        if (CurrentSlot != null)
        {
            CurrentSlot.ClearSlot();
            CurrentSlot = null;
        }

        // Bring card to top of canvas
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        SetRaycastTarget(false);

        // Disable hover scaling while draggingі
        if (scaler != null)
            scaler.SetDragging(true);

        ToggleOtherCardsInSlotsRaycasts(false);
    }

    private void ToggleOtherCardsInSlotsRaycasts(bool value)
    {
        if (cardDeckController != null)
            cardDeckController.ToggleCardsRaycasts(value, this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_canDrag) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_canDrag) return;

        SetRaycastTarget(true);
        ToggleOtherCardsInSlotsRaycasts(true);

        if (scaler != null)
            scaler.SetDragging(false);

        if (CurrentSlot == null)
            ReturnHome();
        
        _canDrag = false;
    }

    public void ReturnHome()
    {
        SetRaycastTarget(true);
        transform.SetParent(homeParent, false);

        rectTransform.anchorMin = originalAnchorMin;
        rectTransform.anchorMax = originalAnchorMax;
        rectTransform.pivot = originalPivot;
        rectTransform.sizeDelta = originalSize;
        rectTransform.localPosition = originalLocalPosition;
        rectTransform.localScale = originalLocalScale;
        rectTransform.localRotation = originalLocalRotation;
        
        if (scaler != null)
            scaler.UpdateHome();
    }

    public void SnapToSlot(RectTransform slot)
    {
        transform.SetParent(slot, false);

        rectTransform.anchorMin =
            rectTransform.anchorMax =
                rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = slot.rect.size;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        // 🔥 ВАЖЛИВО
        if (scaler != null)
            scaler.UpdateHome();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var mod = eventData.pointerDrag?.GetComponent<ModDragHandler>();
        if (mod != null)
            mod.AttachToCard(transform as RectTransform);
    }
}