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
    public event Action<CardSelectionHandler> OnCardReturnedToDraft;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Vector3 _originalPosition;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private CardScaler _cardScaler;
    private bool _isInSlot = false;
    private float _slotScaleFactor = 1f;

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

        // «бер≥гаЇмо початкову позиц≥ю в пол≥ драфту
        _originalPosition = _rectTransform.anchoredPosition;
        _originalParent = _rectTransform.parent;
        _originalSiblingIndex = _rectTransform.GetSiblingIndex();
    }

    public void SetSelection(SelectionMode newMode)
    {
        CurrentMode = newMode;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsDraggable()) return;

        // «бер≥гаЇмо поточну позиц≥ю перед початком перет€гуванн€
        if (!_isInSlot)
        {
            _originalPosition = _rectTransform.anchoredPosition;
            _originalParent = _rectTransform.parent;
            _originalSiblingIndex = _rectTransform.GetSiblingIndex();
        }

        if (_cardScaler != null)
        {
            // ѕри перет€гуванн≥ повертаЇмо нормальний масштаб
            _cardScaler.ForceScale(_cardScaler.InitialScale * DragScaleMultiplier);
        }

        if (DragCanvasGroup != null)
        {
            DragCanvasGroup.alpha = 0.8f;
            DragCanvasGroup.blocksRaycasts = false;
        }

        _rectTransform.SetParent(_canvas.transform);
        _rectTransform.SetAsLastSibling();

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

        // —кидаЇмо в≥зуальн≥ ефекти
        if (_cardScaler != null)
        {
            _cardScaler.ResetToInitialScale();
        }

        if (DragCanvasGroup != null)
        {
            DragCanvasGroup.alpha = 1f;
            DragCanvasGroup.blocksRaycasts = true;
        }

        // ЎукаЇмо DropSlot п≥д курсором
        DropSlot dropSlot = FindDropSlotUnderPointer(eventData);

        if (dropSlot != null)
        {
            // якщо знайшли слот - перем≥щуЇмо картку туди
            dropSlot.AcceptCard(this);
            OnCardDropped?.Invoke(this, dropSlot);
        }
        else
        {
            // якщо слот не знайшли - повертаЇмо картку
            ReturnToDraftArea();
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
        _isInSlot = false;
    }

    public void ReturnToDraftArea()
    {
        _rectTransform.SetParent(_originalParent);
        _rectTransform.SetSiblingIndex(_originalSiblingIndex);
        _rectTransform.anchoredPosition = _originalPosition;
        _rectTransform.localScale = Vector3.one;
        _isInSlot = false;

        SetSelection(SelectionMode.None);
        OnCardReturnedToDraft?.Invoke(this);

        Debug.Log($"Card {CardData.CharacterName} returned to draft area");
    }

    public void MoveToSlot(Transform slotTransform, float scaleFactor = 1f)
    {
        _rectTransform.SetParent(slotTransform);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.localPosition = Vector3.zero;

        // «астосовуЇмо масштаб слота
        _slotScaleFactor = scaleFactor;
        _rectTransform.localScale = Vector3.one * scaleFactor;

        _isInSlot = true;
    }

    private bool IsDraggable()
    {
        return gameObject.activeInHierarchy && CardData != null && enabled;
    }

    // ƒодатков≥ методи дл€ управл≥нн€ станом
    public bool IsInSlot()
    {
        return _isInSlot;
    }

    public void ForceReturnToDraft()
    {
        ReturnToDraftArea();
    }
}