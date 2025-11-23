// TraitCardHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;

/// <summary>
/// Обробник drag&drop для карток рис (TraitData).
/// Аналог CardSelectionHandler, але для рис.
/// </summary>
public class TraitCardHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TraitData TraitData { get; private set; }

    [Header("Drag & Drop Settings")]
    public float DragScaleMultiplier = 1.1f;
    public CanvasGroup DragCanvasGroup;

    public event Action<TraitCardHandler> OnTraitBeginDrag;
    public event Action<TraitCardHandler> OnTraitEndDrag;
    public event Action<TraitCardHandler, TraitSlot> OnTraitDropped;
    public event Action<TraitCardHandler> OnTraitReturnedToPool;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Vector3 _originalPosition;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private bool _isInSlot = false;
    private TraitSlot _currentSlot;
    private LayoutElement _layoutElement;

    [Header("UI References")]
    public Text TraitNameText;
    public Text CostText;
    public Text DescriptionText;
    public Image TraitIconImage;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _layoutElement = GetComponent<LayoutElement>();

        if (DragCanvasGroup == null)
            DragCanvasGroup = GetComponent<CanvasGroup>();
    }

    public void Initialize(TraitData data)
    {
        TraitData = data;
        UpdateUI();
        _originalPosition = _rectTransform.anchoredPosition;
        _originalParent = _rectTransform.parent;
        _originalSiblingIndex = _rectTransform.GetSiblingIndex();
    }

    private void UpdateUI()
    {
        if (TraitData == null) return;

        if (TraitNameText != null)
            TraitNameText.text = TraitData.TraitName;

        if (CostText != null)
            CostText.text = $"{TraitData.Cost} UP";

        if (DescriptionText != null)
            DescriptionText.text = TraitData.Description;

        // TODO: якщо є іконка в TraitData, встановити її
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsDraggable()) return;

        if (!_isInSlot)
        {
            _originalPosition = _rectTransform.anchoredPosition;
            _originalParent = _rectTransform.parent;
            _originalSiblingIndex = _rectTransform.GetSiblingIndex();
        }
        else
        {
            if (_currentSlot != null)
            {
                _currentSlot.ClearTraitWithoutReturning();
                _currentSlot = null;
            }
        }

        if (_layoutElement != null)
        {
            _layoutElement.ignoreLayout = true;
        }

        if (DragCanvasGroup != null)
        {
            DragCanvasGroup.alpha = 0.8f;
            DragCanvasGroup.blocksRaycasts = false;
        }

        _rectTransform.SetParent(_canvas.transform);
        _rectTransform.SetAsLastSibling();

        OnTraitBeginDrag?.Invoke(this);
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

        if (DragCanvasGroup != null)
        {
            DragCanvasGroup.alpha = 1f;
            DragCanvasGroup.blocksRaycasts = true;
        }

        TraitSlot dropSlot = FindTraitSlotUnderPointer(eventData);

        if (dropSlot != null && dropSlot != _currentSlot)
        {
            Debug.Log($"Trait dropped onto slot: {dropSlot.name}");
            dropSlot.AcceptTrait(this);
            OnTraitDropped?.Invoke(this, dropSlot);
        }
        else
        {
            Debug.Log("No valid TraitSlot found - returning to original position");
            ReturnToPool();
        }

        if (_layoutElement != null)
        {
            _layoutElement.ignoreLayout = false;
        }

        OnTraitEndDrag?.Invoke(this);
    }

    private TraitSlot FindTraitSlotUnderPointer(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            TraitSlot slot = result.gameObject.GetComponentInParent<TraitSlot>();
            if (slot != null && slot.enabled && slot.CanAcceptTrait(this))
            {
                return slot;
            }
        }

        return null;
    }

    public void ReturnToPool()
    {
        _rectTransform.SetParent(_originalParent);
        _rectTransform.SetSiblingIndex(_originalSiblingIndex);
        _rectTransform.anchoredPosition = _originalPosition;
        _rectTransform.localScale = Vector3.one;
        _isInSlot = false;
        _currentSlot = null;

        OnTraitReturnedToPool?.Invoke(this);
        Debug.Log($"Trait {TraitData?.TraitName} returned to pool");
    }

    public void MoveToSlot(Transform slotTransform, TraitSlot slot)
    {
        _rectTransform.SetParent(slotTransform);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.localPosition = Vector3.zero;

        _isInSlot = true;
        _currentSlot = slot;

        if (_layoutElement != null)
        {
            _layoutElement.ignoreLayout = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        Debug.Log($"Trait {TraitData?.TraitName} moved to slot {slotTransform.name}");
    }

    public void OnPlacedInSlot(TraitSlot slot)
    {
        _isInSlot = true;
        _currentSlot = slot;
    }

    public void OnRemovedFromSlot()
    {
        _isInSlot = false;
        _currentSlot = null;
    }

    private bool IsDraggable()
    {
        return gameObject.activeInHierarchy && TraitData != null && enabled;
    }

    public bool IsInSlot() => _isInSlot;
    public TraitSlot GetCurrentSlot() => _currentSlot;
}

