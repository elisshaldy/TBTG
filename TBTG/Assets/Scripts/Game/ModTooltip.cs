using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ModTooltip : MonoBehaviour
{
    private static ModTooltip _instance;
    public static ModTooltip Instance => _instance;

    [SerializeField] private GameObject _tooltipPanel; // background
    [Header("Base")]
    [SerializeField] private LocalizationLabel _nameText;
    [SerializeField] private LocalizationLabel _descriptionText;
    [SerializeField] private LocalizationLabel _typeText;
    [Header("Critical Variant")]
    [SerializeField] private LocalizationLabel _nameTextCritical;
    [SerializeField] private LocalizationLabel _descriptionTextCritical;
    [SerializeField] private LocalizationLabel _typeTextCritical;
    [Space(20)]
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Vector2 _offset = new Vector2(20, -20);
    [SerializeField] private int _paddingHorizontal = 30;
    [SerializeField] private int _paddingVertical = 40;
    
    [Header("Fade Animation")]
    [SerializeField] private float _fadeDuration = 0.15f;   

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _canvas = GetComponentInParent<Canvas>();
            _canvasGroup = _tooltipPanel.GetComponent<CanvasGroup>();
            
            if (_canvasGroup == null) 
                _canvasGroup = _tooltipPanel.AddComponent<CanvasGroup>();
            
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // Ensure the pivot is top-left for easier boundary logic
            _rectTransform.pivot = new Vector2(0, 1);

            SetupDynamicLayout();
            Hide();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupDynamicLayout()
    {
        // Automatically setup layout components if missing to prevent overlapping
        var group = _tooltipPanel.GetComponent<VerticalLayoutGroup>();
        if (group == null)
            group = _tooltipPanel.AddComponent<VerticalLayoutGroup>();

        group.childControlHeight = true;
        group.childControlWidth = true;
        group.childForceExpandHeight = false; // Key for fitting content tightly
        group.childForceExpandWidth = true;
        group.spacing = 10; 
        
        // Apply padding from serialized fields
        group.padding = new RectOffset(_paddingHorizontal, _paddingHorizontal, _paddingVertical, _paddingVertical);

        var fitter = _tooltipPanel.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = _tooltipPanel.AddComponent<ContentSizeFitter>();

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        // Keep horizontal constrained or preferred depending on design. 
        // We set Preferred here to let it grow, but usually width is fixed.
        // If you want fixed width: Set horizontalFit = Unconstrained and set Width in Inspector.
        // For now, let's assume vertical dynamic is the priority.
        if (fitter.horizontalFit == ContentSizeFitter.FitMode.Unconstrained) 
        { 
             // Keep it unconstrained if user set it so, otherwise...
             // fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; 
        }
    }

    public void Show(ModData modData, Vector2 position)
    {
        if (modData == null) return;

        _tooltipPanel.SetActive(true);
        _rectTransform.SetAsLastSibling();
        
        // --- Base Mod ---
        _nameText.SetKey(modData.ModificatorName);
        _descriptionText.SetKey(modData.ModificatorDescription);

        if (_typeText != null)
        {
            string typeKey = GetTypeKey(modData.ModType);
            _typeText.SetKey(typeKey);
        }

        // --- Critical Variant ---
        bool hasCritical = modData.Critical != null && modData.Critical.Count > 0;
        
        // Show/Hide critical section parents (assuming labels are on child objects) or just labels themselves
        // For safer usage, we check if text components are assigned
        if (hasCritical)
        {
            ModData critData = modData.Critical[0];

            if (_nameTextCritical != null)
            {
                _nameTextCritical.gameObject.SetActive(true);
                _nameTextCritical.SetKey(critData.ModificatorName);
            }
            if (_descriptionTextCritical != null)
            {
                _descriptionTextCritical.gameObject.SetActive(true);
                _descriptionTextCritical.SetKey(critData.ModificatorDescription);
            }
            if (_typeTextCritical != null)
            {
                _typeTextCritical.gameObject.SetActive(true);
                string critTypeKey = GetTypeKey(critData.ModType); // Should be Critical
                _typeTextCritical.SetKey(critTypeKey);
            }
        }
        else
        {
            if (_nameTextCritical != null) _nameTextCritical.gameObject.SetActive(false);
            if (_descriptionTextCritical != null) _descriptionTextCritical.gameObject.SetActive(false);
            if (_typeTextCritical != null) _typeTextCritical.gameObject.SetActive(false);
        }

        // Wait for end of frame to ensure TMP has updated its text bounds before rebuilding layout
        StartCoroutine(RebuildLayout());
        
        StartFade(1f);
    }

    private IEnumerator RebuildLayout()
    {
        yield return new WaitForEndOfFrame();
        // Rebuild the Panel which holds the content
        LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipPanel.transform as RectTransform); 
        if (_tooltipPanel.transform != _rectTransform)
        {
             LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        }
        
        // Sometimes nested layouts need a second kick
        yield return null; 
        LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipPanel.transform as RectTransform);
        if (_tooltipPanel.transform != _rectTransform)
        {
             LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        }
        
        UpdatePosition(Input.mousePosition);
    }

    private string GetTypeKey(ModType type)
    {
        return type switch
        {
            ModType.Active => "enum_mod_am",
            ModType.Passive => "enum_mod_pm",
            ModType.Critical => "enum_mod_cm",
            _ => ""
        };
    }

    public void UpdatePosition(Vector2 screenPosition)
    {
        if (_rectTransform.parent == null) return;
        
        RectTransform parentRect = _rectTransform.parent as RectTransform;
        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        // 1. Convert mouse screen position to local space of the parent
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, cam, out Vector2 localMousePos);

        // 2. Get dimensions in UI Units (not pixels)
        float width = _rectTransform.rect.width;
        float height = _rectTransform.rect.height;

        // 3. Initial Target Position (Top-Left pivot 0, 1)
        Vector2 targetPos = localMousePos + _offset;

        // 4. Horizontal Flip logic (using local space bounds)
        if (targetPos.x + width > parentRect.rect.xMax)
        {
            targetPos.x = localMousePos.x - width - _offset.x;
        }

        // 5. Vertical Flip logic (using local space bounds)
        // Since pivot is Top (1), targetPos.y is the Top edge. Bottom edge is targetPos.y - height.
        if (targetPos.y - height < parentRect.rect.yMin)
        {
            targetPos.y = localMousePos.y + height - _offset.y;
        }

        // 6. Clamp to parent boundaries to ensure it never leaves the UI area
        targetPos.x = Mathf.Clamp(targetPos.x, parentRect.rect.xMin, parentRect.rect.xMax - width);
        targetPos.y = Mathf.Clamp(targetPos.y, parentRect.rect.yMin + height, parentRect.rect.yMax);

        // 7. Apply to localPosition
        _rectTransform.localPosition = targetPos;
    }

    public void Hide()
    {
        StartFade(0f);
    }
    
    private void StartFade(float targetAlpha)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);
        
        if (!gameObject.activeInHierarchy)
        {
            _canvasGroup.alpha = targetAlpha;
            if (targetAlpha == 0f)
                _tooltipPanel.SetActive(false);
            return;
        }
        
        _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }
    
    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;
        
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fadeDuration;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        
        _canvasGroup.alpha = targetAlpha;
        
        if (targetAlpha == 0f)
            _tooltipPanel.SetActive(false);
        
        _fadeCoroutine = null;
    }
}
