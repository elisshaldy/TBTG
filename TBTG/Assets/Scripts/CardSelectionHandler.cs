using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;

public class CardSelectionHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CharacterData CardData { get; private set; }
    public SelectionMode CurrentMode { get; private set; } = SelectionMode.None;

    [Header("Drag & Drop Settings")]
    public float DragScaleMultiplier = 1.1f;
    public CanvasGroup DragCanvasGroup;

    public event Action<CardSelectionHandler> OnCardBeginDrag;
    public event Action<CardSelectionHandler> OnCardEndDrag;
    public event Action<CardSelectionHandler, DropSlot> OnCardDropped;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Vector3 _originalPosition;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private CardScaler _cardScaler;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _cardScaler = GetComponent<CardScaler>();

        if (DragCanvasGroup == null)
            DragCanvasGroup = GetComponent<CanvasGroup>();
    }

    public void Initialize(CharacterData data)
    {
        CardData = data;
        var cardUI = GetComponent<CharacterCardUI>();
        if (cardUI != null)
        {
            cardUI.DisplayCharacter(data);
        }
        SetSelection(SelectionMode.None);
    }

    public void SetSelection(SelectionMode newMode)
    {
        CurrentMode = newMode;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsDraggable()) return;

        _originalPosition = _rectTransform.anchoredPosition;
        _originalParent = _rectTransform.parent;
        _originalSiblingIndex = _rectTransform.GetSiblingIndex();

        if (_cardScaler != null)
        {
            _cardScaler.ForceScale(_cardScaler.InitialScale * DragScaleMultiplier);
        }

        if (DragCanvasGroup != null)
        {
            DragCanvasGroup.alpha = 0.8f;
            DragCanvasGroup.blocksRaycasts = false;
        }

        _rectTransform.SetParent(_canvas.transform);
        _rectTransform.SetAsLastSibling();

        Debug.Log($"Started dragging: {CardData.CharacterName}");
        OnCardBeginDrag?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDraggable()) return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            _rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 worldPoint))
        {
            _rectTransform.position = worldPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDraggable()) return;

        Debug.Log($"Ended dragging: {CardData.CharacterName}");

        // Скидаємо візуальні ефекти
        if (_cardScaler != null)
        {
            _cardScaler.ResetToInitialScale();
        }

        if (DragCanvasGroup != null)
        {
            DragCanvasGroup.alpha = 1f;
            DragCanvasGroup.blocksRaycasts = true;
        }

        // Шукаємо DropSlot
        DropSlot dropSlot = FindDropSlotUnderPointer(eventData);

        if (dropSlot != null)
        {
            Debug.Log($"Found slot: {dropSlot.name}, calling CanAcceptCard...");
            if (dropSlot.CanAcceptCard(this))
            {
                Debug.Log($"Slot accepted card, calling AcceptCard...");
                dropSlot.AcceptCard(this);
                OnCardDropped?.Invoke(this, dropSlot);
                Debug.Log($"Card {CardData.CharacterName} successfully dropped in slot {dropSlot.name}");
            }
            else
            {
                Debug.Log($"Slot {dropSlot.name} rejected card");
                ReturnToOriginalPosition();
            }
        }
        else
        {
            Debug.Log("No DropSlot found under pointer");
            ReturnToOriginalPosition();
        }

        OnCardEndDrag?.Invoke(this);
    }

    private DropSlot FindDropSlotUnderPointer(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            DropSlot slot = result.gameObject.GetComponent<DropSlot>();
            if (slot != null)
            {
                return slot;
            }
        }

        return null;
    }

    public void ReturnToOriginalPosition()
    {
        _rectTransform.SetParent(_originalParent);
        _rectTransform.SetSiblingIndex(_originalSiblingIndex);
        _rectTransform.anchoredPosition = _originalPosition;
        _rectTransform.localScale = Vector3.one;
    }

    public void MoveToSlot(Transform slotTransform)
    {
        _rectTransform.SetParent(slotTransform);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.localPosition = Vector3.zero;
        _rectTransform.localScale = Vector3.one;
    }

    private bool IsDraggable()
    {
        return gameObject.activeInHierarchy && CardData != null && enabled;
    }
}