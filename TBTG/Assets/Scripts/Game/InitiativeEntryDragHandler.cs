using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InitiativeEntryDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int PairID { get; set; }
    
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Transform _originalParent;
    private int _originalIndex;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InitiativeSystem.Instance != null)
            InitiativeSystem.Instance.ResetDropFlag();

        _originalParent = transform.parent;
        _originalIndex = transform.GetSiblingIndex();
        
        // Тимчасово виносимо на самий верх Canvas, щоб бачити перетягування
        transform.SetParent(_canvas.transform, true);
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        // Більше не видаляємо! Якщо кинули мимо, просто кажемо системі 
        // перемалювати чергу, щоб іконка повернулася на місце.
        if (InitiativeSystem.Instance != null)
        {
             InitiativeSystem.Instance.UpdateInitiativeUI();
        }

        // Завжди знищуємо цей хендлер, бо ініціатива створює нові плашки при UpdateUI
        Destroy(gameObject);
    }
}
