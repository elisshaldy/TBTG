// CardScaler.cs - ФІКСОВАНА ВЕРСІЯ
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

public class CardScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Налаштування масштабування")]
    public Vector3 HoverScale = new Vector3(1.2f, 1.2f, 1.2f);
    public float AnimationDuration = 0.2f;

    [Header("Налаштування сортування")]
    public int SortOrderIncrease = 10;

    private Coroutine _scaleCoroutine;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private int _originalSortOrder;
    private Vector3 _initialScale = Vector3.one; // Приватна, керується через метод

    // Публічна властивість тільки для читання
    public Vector3 InitialScale { get; private set; } = Vector3.one;

    private bool _isDragging = false;
    private Vector3 _dragOffset;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponent<Canvas>();

        // ОРИГІНАЛЬНА ЛОГІКА З БАЗОВОГО КОДУ
        if (_canvas == null)
        {
            _canvas = gameObject.AddComponent<Canvas>();
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        _canvas.overrideSorting = true;

        // Знаходимо батьківський Canvas, щоб успадкувати базовий порядок сортування
        Canvas parentCanvas = transform.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.overrideSorting)
        {
            _originalSortOrder = parentCanvas.sortingOrder;
        }
        else
        {
            _originalSortOrder = _canvas.sortingOrder;
        }

        _canvas.sortingOrder = _originalSortOrder;

        // Встановлюємо початковий масштаб
        _rectTransform.localScale = InitialScale;
    }

    /// <summary>
    /// Встановлює початковий розмір, який розраховує PlayerCardManager.
    /// Цей метод викликається одразу після створення картки.
    /// </summary>
    public void SetInitialScale(Vector3 scale)
    {
        InitialScale = scale;
        _initialScale = scale; // Зберігаємо копію
        // Застосовуємо початковий масштаб негайно
        _rectTransform.localScale = InitialScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);

        // 1. Збільшуємо sort order, щоб картка була зверху
        _canvas.sortingOrder = _originalSortOrder + SortOrderIncrease;

        // 2. Починаємо анімацію масштабування
        _scaleCoroutine = StartCoroutine(ScaleToTarget(HoverScale, true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);

        // Починаємо анімацію повернення до початкового масштабу
        _scaleCoroutine = StartCoroutine(ScaleToTarget(InitialScale, false));
    }

    // ОРИГІНАЛЬНІ МЕТОДИ DRAG З БАЗОВОГО КОДУ
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _dragOffset = transform.position - Input.mousePosition;
        _canvas.sortingOrder = _originalSortOrder + SortOrderIncrease;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            transform.position = Input.mousePosition + _dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        // Тут додати логіку перевірки зони скидання
    }

    /// <summary>
    /// Корутина для плавного переходу масштабу.
    /// ФІКСОВАНА ВЕРСІЯ - правильно скидає sort order
    /// </summary>
    private IEnumerator ScaleToTarget(Vector3 targetScale, bool isEntering)
    {
        float elapsedTime = 0;
        Vector3 startingScale = _rectTransform.localScale;

        while (elapsedTime < AnimationDuration)
        {
            float t = elapsedTime / AnimationDuration;
            // Використовуємо SmoothStep для більш природної кривої анімації
            t = Mathf.SmoothStep(0.0f, 1.0f, t);
            _rectTransform.localScale = Vector3.Lerp(startingScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Фіналізація
        _rectTransform.localScale = targetScale;
        _scaleCoroutine = null;

        // КРИТИЧНО ВАЖЛИВО: Якщо це анімація повернення (Exit), скидаємо sort order
        if (!isEntering)
        {
            _canvas.sortingOrder = _originalSortOrder;
        }
    }

    /// <summary>
    /// Новий метод для примусового встановлення масштабу
    /// БЕЗ оновлення InitialScale, щоб уникнути циклічного збільшення
    /// </summary>
    public void ForceScale(Vector3 scale)
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }

        _rectTransform.localScale = scale;
        // НЕ оновлюємо InitialScale тут, щоб уникнути проблеми
    }

    /// <summary>
    /// Метод для примусового скидання до початкового масштабу
    /// </summary>
    public void ResetToInitialScale()
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }

        _rectTransform.localScale = InitialScale;
        _canvas.sortingOrder = _originalSortOrder;
    }
}