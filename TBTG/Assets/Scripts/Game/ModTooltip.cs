using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ModTooltip : MonoBehaviour
{
    private static ModTooltip _instance;
    public static ModTooltip Instance => _instance;

    [SerializeField] private GameObject _tooltipPanel;
    [SerializeField] private LocalizationLabel _nameText;
    [SerializeField] private LocalizationLabel _descriptionText;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Vector2 _offset = new Vector2(20, -20);
    
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

            Hide();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Show(ModData modData, Vector2 position)
    {
        if (modData == null) return;

        _tooltipPanel.SetActive(true);
        _rectTransform.SetAsLastSibling();
        _nameText.SetKey(modData.ModificatorName);
        _descriptionText.SetKey(modData.ModificatorDescription);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        UpdatePosition(position);
        
        StartFade(1f);
    }

    public void UpdatePosition(Vector2 position)
    {
        Vector2 localPoint;
        RectTransform parentRect = _rectTransform.parent as RectTransform;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            position, 
            _canvas.worldCamera, 
            out localPoint);

        Vector2 size = _rectTransform.sizeDelta;
        Vector2 finalOffset = _offset;

        // Dynamic flipping based on screen half
        // If mouse in right half of screen, move tooltip to the left
        if (position.x + size.x + _offset.x > Screen.width)
        {
            finalOffset.x = -size.x - _offset.x;
        }

        // If mouse in bottom half of screen (near bottom), move tooltip up
        if (position.y - size.y + _offset.y < 0)
        {
            finalOffset.y = size.y - _offset.y;
        }

        _rectTransform.localPosition = localPoint + finalOffset;
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
