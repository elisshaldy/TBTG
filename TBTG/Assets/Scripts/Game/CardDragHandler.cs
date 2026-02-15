using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public CardSlot CurrentSlot { get; set; }
    public CardSlot LastSlot { get; private set; }

    public bool IsLockedInSlot => gameSceneState != null && gameSceneState.CurrentStep == GameSetupStep.Map;
    public int CardID { get; set; } = -1;
    public int OwnerID { get; set; } = -1;
    public int PairID { get; set; } = -1;

    public CardDragHandler PartnerCard { get; set; }
    public bool IsPassive { get; set; }
    private bool _isDeactivated = false;

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
        if (_isDeactivated)
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
            if (cardImage != null) cardImage.raycastTarget = false;
            return;
        }

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = value;
        
        if (cardImage != null) cardImage.raycastTarget = value;
    }

    public void SetLastSlot(CardSlot slot)
    {
        LastSlot = slot;
    }

    public void SetDeactivated(bool value)
    {
        _isDeactivated = value;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            // Removed transparency as per user request
            canvasGroup.blocksRaycasts = !value;
        }
        if (cardImage != null) cardImage.raycastTarget = !value;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        _canDrag = false;

        if (gameSceneState != null && 
            gameSceneState.CurrentStep != GameSetupStep.Cards && 
            gameSceneState.CurrentStep != GameSetupStep.Map)
        {
            eventData.pointerDrag = null;
            return;
        }

        _canDrag = true;
        
        // Passive cards can be dragged (to swap), but Cannot be placed on field.
        // The check for placement is in TryPlaceOnMapTile.

        if (_isDeactivated)
        {
            _canDrag = false;
            eventData.pointerDrag = null;
            return;
        }

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
        PlayerCameraController.BlockCameraControl = true;

        if (canvasGroup != null) canvasGroup.alpha = 0.5f;
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

        if (IsLockedInSlot)
        {
            UpdatePreview(eventData);
        }
    }

    private void UpdatePreview(PointerEventData eventData)
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile == null) tile = hit.collider.GetComponentInParent<Tile>();

            if (tile != null)
            {
                if (CharacterPlacementManager.Instance != null)
                {
                    CharacterPlacementManager.Instance.ShowPreview(this, tile);
                    return;
                }
            }
        }

        if (CharacterPlacementManager.Instance != null)
            CharacterPlacementManager.Instance.HidePreview();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_canDrag) return;

        SetRaycastTarget(true);
        ToggleOtherCardsInSlotsRaycasts(true);

        if (scaler != null)
            scaler.SetDragging(false);

        bool placedSuccessfully = false;
        if (IsLockedInSlot)
        {
            placedSuccessfully = TryPlaceOnMapTile(eventData);
            if (placedSuccessfully && cardDeckController != null)
            {
                cardDeckController.MakeActive(this);
            }
        }

        if (CurrentSlot == null)
        {
            if (IsLockedInSlot && LastSlot != null)
            {
                // Якщо не вдалося поставити на нову клітинку, ми просто повертаємо картку в слот,
                // але БІЛЬШЕ НЕ видаляємо персонажа з поля (ClearPlacement), 
                // щоб він залишався на своїй старій позиції.

                LastSlot.SetCardManually(this);
            }
            else
            {
                ReturnHome();
            }
        }
        
        _canDrag = false;
        PlayerCameraController.BlockCameraControl = false;
        if (canvasGroup != null) canvasGroup.alpha = 1.0f;

        if (CharacterPlacementManager.Instance != null)
            CharacterPlacementManager.Instance.HidePreview();
    }

    private bool TryPlaceOnMapTile(PointerEventData eventData)
    {
        if (Camera.main == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile == null) tile = hit.collider.GetComponentInParent<Tile>();

            if (tile != null)
            {
                if (CharacterPlacementManager.Instance != null)
                {
                    return CharacterPlacementManager.Instance.TryPlaceCharacter(this, tile);
                }
            }
        }
        return false;
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