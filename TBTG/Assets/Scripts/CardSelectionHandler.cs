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
    private LayoutElement _layoutElement;
    private DropSlot _currentSlot;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _cardScaler = GetComponent<CardScaler>();
        _layoutElement = GetComponent<LayoutElement>();

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

        // �������� ��������� �������, ���� ������ �� � ����
        if (!_isInSlot)
        {
            _originalPosition = _rectTransform.anchoredPosition;
            _originalParent = _rectTransform.parent;
            _originalSiblingIndex = _rectTransform.GetSiblingIndex();
        }
        else
        {
            // ���� ������ � ���� - ��������� ����
            if (_currentSlot != null)
            {
                _currentSlot.ClearCardWithoutReturning();
                _currentSlot = null;
            }
        }

        // ������������ ��� �������������
        if (_layoutElement != null)
        {
            _layoutElement.ignoreLayout = true;
        }

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

        // ������� �������� ������ �������������
        if (_cardScaler != null)
        {
            _cardScaler.ResetToInitialScale();
        }

        if (DragCanvasGroup != null)
        {
            DragCanvasGroup.alpha = 1f;
            DragCanvasGroup.blocksRaycasts = true;
        }

        // На цьому етапі стандартна система EventSystem/IDropHandler вже викличе
        // DropSlot.OnDrop, якщо ми відпустили картку над слотом.
        // DropSlot.AcceptCard викличе OnPlacedInSlot(this) і виставить _isInSlot = true.
        //
        // Тому тут достатньо перевірити, чи ми опинились у слоті:
        if (!_isInSlot)
        {
            Debug.Log("Card not placed in any slot - returning to original position");
            ReturnToDraftArea();
        }

        // �������� Layout Element �����
        if (_layoutElement != null)
        {
            _layoutElement.ignoreLayout = false;
        }

        OnCardEndDrag?.Invoke(this);
    }

    private DropSlot FindDropSlotUnderPointer(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // ВАЖЛИВО:
            // Raycast зазвичай повертає дочірні елементи (Image, Text тощо),
            // а не сам об'єкт зі скриптом DropSlot, тому шукаємо в батьках.
            DropSlot slot = result.gameObject.GetComponentInParent<DropSlot>();
            if (slot != null && slot.enabled)
            {
                return slot;
            }
        }

        return null;
    }

    public void ReturnToDraftArea()
    {
        _rectTransform.SetParent(_originalParent);
        _rectTransform.SetSiblingIndex(_originalSiblingIndex);
        _rectTransform.anchoredPosition = _originalPosition;
        _rectTransform.localScale = Vector3.one;
        _isInSlot = false;
        _currentSlot = null;

        SetSelection(SelectionMode.None);
        OnCardReturnedToDraft?.Invoke(this);

        Debug.Log($"Card {CardData.CharacterName} returned to draft area");
    }

    public void MoveToSlot(Transform slotTransform, DropSlot slot, float scaleFactor = 1f)
    {
        _rectTransform.SetParent(slotTransform);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.localPosition = Vector3.zero;

        _slotScaleFactor = scaleFactor;
        _rectTransform.localScale = Vector3.one * scaleFactor;

        _isInSlot = true;
        _currentSlot = slot;

        // ������������ ��� ����������� ����������� � ����
        if (_layoutElement != null)
        {
            _layoutElement.ignoreLayout = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

        Debug.Log($"Card {CardData.CharacterName} moved to slot {slotTransform.name}");
    }

    /// <summary>
    /// ����������� ���� ������ ������ �������� � ����
    /// </summary>
    public void OnPlacedInSlot(DropSlot slot)
    {
        _isInSlot = true;
        _currentSlot = slot;

        SelectionMode newMode = (slot.SlotType == SlotType.Active) ? SelectionMode.Visible : SelectionMode.Hidden;
        SetSelection(newMode);
    }

    /// <summary>
    /// ����������� ���� ������ �������� � �����
    /// </summary>
    public void OnRemovedFromSlot()
    {
        _isInSlot = false;
        _currentSlot = null;
        SetSelection(SelectionMode.None);
    }

    private bool IsDraggable()
    {
        return gameObject.activeInHierarchy && CardData != null && enabled;
    }

    public bool IsInSlot()
    {
        return _isInSlot;
    }

    public DropSlot GetCurrentSlot()
    {
        return _currentSlot;
    }
}