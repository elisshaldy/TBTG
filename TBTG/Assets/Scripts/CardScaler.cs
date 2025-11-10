using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI; // Додано для роботи з Canvas

// Скрипт для плавного масштабування UI-елемента (картки) при наведенні миші.
public class CardScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Базовий розмір, до якого картка повертається після відведення курсору. Встановлюється PlayerCardManager.")]
    public Vector3 InitialScale { get; private set; } = Vector3.one;

    [Header("Налаштування масштабування")]

    [Tooltip("Розмір, до якого картка збільшується при наведенні курсору.")]
    public Vector3 HoverScale = new Vector3(1.2f, 1.2f, 1.2f);

    [Tooltip("Тривалість анімації переходу в секундах.")]
    public float AnimationDuration = 0.2f;

    [Header("Налаштування сортування")]
    [Tooltip("На скільки збільшити sort order при наведенні")]
    public int SortOrderIncrease = 10;

    private Coroutine _scaleCoroutine;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private int _originalSortOrder;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponent<Canvas>();

        // !!! ОПТИМІЗАЦІЯ: Краще, щоб ці компоненти були на префабі, але залишаємо ваш код для динамічного додавання !!!
        if (_canvas == null)
        {
            _canvas = gameObject.AddComponent<Canvas>();
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        _canvas.overrideSorting = true; // Дозволяє контролювати сортування

        // Знаходимо батьківський Canvas, щоб успадкувати базовий порядок сортування (якщо він є)
        Canvas parentCanvas = transform.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.overrideSorting)
        {
            // Якщо батьківський Canvas перевизначає сортування, використовуємо його sortingOrder як базу
            _originalSortOrder = parentCanvas.sortingOrder;
        }
        else
        {
            // Інакше використовуємо 0 або поточний порядок Canvas
            _originalSortOrder = _canvas.sortingOrder;
        }

        // Встановлюємо початковий порядок сортування для новоствореного Canvas
        _canvas.sortingOrder = _originalSortOrder;
    }

    /// <summary>
    /// Встановлює початковий розмір, який розраховує PlayerCardManager.
    /// Цей метод викликається одразу після створення картки.
    /// </summary>
    /// <param name="scale">Розрахований початковий масштаб.</param>
    public void SetInitialScale(Vector3 scale)
    {
        InitialScale = scale;
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

        // !!! Ми НЕ скидаємо sort order тут, щоб велика картка не пішла під сусідні, 
        // поки вона анімується назад до маленького розміру.

        // Починаємо анімацію повернення до початкового масштабу, і скинемо order в кінці корутини.
        _scaleCoroutine = StartCoroutine(ScaleToTarget(InitialScale, false));
    }

    /// <summary>
    /// Корутина для плавного переходу масштабу.
    /// </summary>
    /// <param name="targetScale">Цільовий масштаб.</param>
    /// <param name="isEntering">Чи відбувається анімація наведення (true) чи відведення (false).</param>
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

        // !!! КРИТИЧНО: Якщо це анімація повернення (Exit), скидаємо sort order !!!
        if (!isEntering)
        {
            _canvas.sortingOrder = _originalSortOrder;
        }
    }
}