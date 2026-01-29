using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CardScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.5f;
    [SerializeField] private float speed = 15f;

    private Vector3 normalScale;
    private Vector3 targetScale;

    private Transform homeParent;
    private int homeSiblingIndex;
    private Vector3 homeLocalPosition;
    private Quaternion homeLocalRotation;

    private Vector3 returnPosition;
    private Quaternion returnRotation;
    private bool returningHome;

    private Canvas canvas;
    private RectTransform rectTransform;
    private RectTransform canvasRect;

    private bool isDraggingSelf;
    private bool isHovered;
    
    private Vector3 initialGlobalScale;
    private static readonly List<CardScaler> ActiveScalers = new List<CardScaler>();

    private void OnEnable()
    {
        ActiveScalers.Add(this);
    }

    private void OnDisable()
    {
        ActiveScalers.Remove(this);
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        normalScale = transform.localScale;
        targetScale = normalScale;

        homeParent = transform.parent;
        homeSiblingIndex = transform.GetSiblingIndex();
        homeLocalPosition = transform.localPosition;
        homeLocalRotation = transform.localRotation;

        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.transform as RectTransform;
        
        initialGlobalScale = transform.localScale;
    }

    private void Update()
    {
        // Плавний скейл
        transform.localScale =
            Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);

        // Плавне повернення на домашнє місце
        if (returningHome)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, returnPosition, Time.deltaTime * speed);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, returnRotation, Time.deltaTime * speed);

            // Збільшена толерантність для завершення анімації
            if (Vector3.Distance(transform.localPosition, returnPosition) < 0.1f &&
                Quaternion.Angle(transform.localRotation, returnRotation) < 1f)
            {
                transform.localPosition = returnPosition;
                transform.localRotation = returnRotation;
                returningHome = false;
            }
        }

        if (isHovered && !isDraggingSelf)
        {
            KeepOnScreen();
        }
    }
    
    public static void ResetAll()
    {
        for (int i = ActiveScalers.Count - 1; i >= 0; i--)
        {
            if (ActiveScalers[i] != null)
            {
                ActiveScalers[i].ResetHover();
            }
        }
    }

    private void KeepOnScreen()
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Camera cam = (canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            minX = Mathf.Min(minX, sp.x);
            maxX = Mathf.Max(maxX, sp.x);
            minY = Mathf.Min(minY, sp.y);
            maxY = Mathf.Max(maxY, sp.y);
        }

        Vector2 shift = Vector2.zero;
        if (minX < 0) shift.x = -minX;
        else if (maxX > Screen.width) shift.x = Screen.width - maxX;

        if (minY < 0) shift.y = -minY;
        else if (maxY > Screen.height) shift.y = Screen.height - maxY;

        if (shift.sqrMagnitude > 0.01f)
        {
            // ВИПРАВКА: Конвертуємо shift із screen в local координати canvas'а
            Vector2 localShift = shift;
            
            if (cam != null)
            {
                // Для world space canvas - конвертуємо screen shift в world
                Vector3 screenPos1 = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);
                Vector3 screenPos2 = screenPos1 + (Vector3)shift;
                
                RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPos2, cam, out Vector3 newWorldPos);
                transform.position = newWorldPos;
            }
            else
            {
                // Для screen space overlay - shift уже в правильних координатах
                rectTransform.anchoredPosition += localShift;
            }
        }
    }

    public void UpdateHome()
    {
        // Якщо карточка ще в процесі повернення, завершимо анімацію
        if (returningHome)
        {
            transform.localPosition = returnPosition;
            transform.localRotation = returnRotation;
            returningHome = false;
        }

        // Не оновлюємо домашню позицію, якщо карточка зараз наведена
        if (isHovered)
            return;

        // Зберігаємо поточне стан як домашній
        homeParent = transform.parent;
        homeSiblingIndex = transform.GetSiblingIndex();
        homeLocalPosition = transform.localPosition;
        homeLocalRotation = transform.localRotation;
        
        // Оновлюємо базовий масштаб
        normalScale = transform.localScale;
        if (!isDraggingSelf)
        {
            targetScale = normalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null || isDraggingSelf || isHovered)
            return;

        isHovered = true;
        if (PersistentMusicManager.Instance != null) PersistentMusicManager.Instance.PlayCardHover();

        // Завершуємо будь-яку поточну анімацію повернення ДО оновлення домашньої позиції
        if (returningHome)
        {
            transform.localPosition = returnPosition;
            transform.localRotation = returnRotation;
            returningHome = false;
        }

        // Зберігаємо позицію ЯК ВОНА ЗАРАЗ Є
        homeParent = transform.parent;
        homeSiblingIndex = transform.GetSiblingIndex();
        homeLocalPosition = transform.localPosition;
        homeLocalRotation = transform.localRotation;

        targetScale = initialGlobalScale * hoverScale;

        // Змінюємо батька на Canvas ДЛЯ ПОРЯДКУ (тільки якщо він не вже там)
        if (transform.parent != canvas.transform)
        {
            transform.SetParent(canvas.transform, true);
        }
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered)
            return;

        isHovered = false;
        targetScale = normalScale;

        // Плавне повернення на домашнє місце
        transform.SetParent(homeParent, true);
        transform.SetSiblingIndex(homeSiblingIndex);

        returnPosition = homeLocalPosition;
        returnRotation = homeLocalRotation;
        returningHome = true;
    }

    public void SetDragging(bool dragging)
    {
        isDraggingSelf = dragging;

        if (dragging)
        {
            isHovered = false;
            targetScale = normalScale;
            transform.localScale = normalScale;
            returningHome = false;
        }
        else
        {
            targetScale = normalScale;
        }
    }

    public void ResetHover()
    {
        OnPointerExit(null);
    }
}