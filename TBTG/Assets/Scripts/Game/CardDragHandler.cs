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
    private Image cardImage;

    private Transform homeParent;

    // UI parameters to restore after drag
    private Vector2 originalSize;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;
    private Vector2 originalAnchoredPosition;
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
        cardImage = GetComponent<Image>();
    }

    public void SetDependencies(GameSceneState sceneState, CardDeckController deckController)
    {
        gameSceneState = sceneState;
        cardDeckController = deckController;

        var modsContainer = GetComponent<ModsCardContainer>();
        if (modsContainer != null) modsContainer.SetDependencies(sceneState);
    }

    public void InitializeHome()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (scaler == null) scaler = GetComponent<CardScaler>();

        homeParent = transform.parent;

        originalSize = rectTransform.sizeDelta;
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;
        originalPivot = rectTransform.pivot;
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalLocalScale = rectTransform.localScale;
        originalLocalRotation = rectTransform.localRotation;

        if (scaler != null)
            scaler.UpdateHome();
    }
    
    public void SetRaycastTarget(bool value)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = value;
        
        if (cardImage != null) cardImage.raycastTarget = value;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        _canDrag = false;

        if (gameSceneState != null && gameSceneState.CurrentStep != GameSetupStep.Cards)
        {
            eventData.pointerDrag = null;
            return;
        }

        _canDrag = true;
        if (PersistentMusicManager.Instance != null) PersistentMusicManager.Instance.PlayCardPickup();

        LastSlot = CurrentSlot;
        if (CurrentSlot != null)
        {
            CurrentSlot.ClearSlot();
            CurrentSlot = null;
        }
        
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        SetRaycastTarget(false);
        
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out globalMousePos))
        {
            rectTransform.position = globalMousePos;
        }

        rectTransform.localRotation = Quaternion.identity;

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
        if (PersistentMusicManager.Instance != null) PersistentMusicManager.Instance.PlayCardReturned();
        SetRaycastTarget(true);
        transform.SetParent(homeParent, false);

        rectTransform.anchorMin = originalAnchorMin;
        rectTransform.anchorMax = originalAnchorMax;
        rectTransform.pivot = originalPivot;
        rectTransform.sizeDelta = originalSize;
        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localScale = originalLocalScale;
        rectTransform.localRotation = originalLocalRotation;
        
        if (scaler != null)
            scaler.UpdateHome();
    }

    public void SnapToSlot(RectTransform slot)
    {
        transform.SetParent(slot, false);

        rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
    
        // Повертаємо розмір, щоб прорахувати скейл
        rectTransform.sizeDelta = originalSize;

        // Вираховуємо коефіцієнт, щоб картка влізла в слот
        float scaleX = slot.rect.width / originalSize.x;
        float scaleY = slot.rect.height / originalSize.y;
        float scale = Mathf.Min(scaleX, scaleY);
    
        // Встановлюємо візуальний масштаб для слота
        rectTransform.localScale = new Vector3(scale, scale, 1f);
        rectTransform.localRotation = Quaternion.identity;

        if (scaler != null)
            scaler.UpdateHome(); 
            
        if (PersistentMusicManager.Instance != null) PersistentMusicManager.Instance.PlayCardPlaced();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var mod = eventData.pointerDrag?.GetComponent<ModDragHandler>();
        if (mod != null)
        {
             mod.AttachToCard(transform as RectTransform);
        }
    }
}