using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class CardFlipController : MonoBehaviour, IPointerClickHandler, IPointerExitHandler
{
    [Header("Sides")]
    [SerializeField] private GameObject _frontSide;
    [SerializeField] private GameObject _backSide;
    [SerializeField] private LocalizationLabel _descriptionLabel;
    [SerializeField] private ScrollRect _descriptionScrollRect;

    [Header("Animation")]
    [SerializeField] private float _flipDuration = 0.4f;
    [SerializeField] private AnimationCurve _flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool _isFlipped = false;
    private bool _isAnimating = false;
    private CardScaler _cardScaler;
    private CardInfo _cardInfo;

    private void Awake()
    {
        _cardScaler = GetComponent<CardScaler>();
        _cardInfo = GetComponent<CardInfo>();

        if (_backSide != null)
        {
            _backSide.SetActive(false);
            // Ensure back side is rotated 180 so it's not mirrored when the card flips
            _backSide.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        
        if (_frontSide != null)
            _frontSide.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Right-click to flip
        if (eventData.button == PointerEventData.InputButton.Right && !_isAnimating)
        {
            StopAllCoroutines();
            StartCoroutine(FlipRoutine(!_isFlipped));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Automatically flip back when mouse leaves, only if already flipped and not busy
        if (_isFlipped && !_isAnimating)
        {
            StopAllCoroutines();
            StartCoroutine(FlipRoutine(false));
        }
    }

    public void ShowFrontSide()
    {
        if (_isAnimating)
        {
            StopAllCoroutines();
            _isAnimating = false;
        }

        _isFlipped = false;
        transform.localRotation = Quaternion.identity;

        if (_frontSide != null) _frontSide.SetActive(true);
        if (_backSide != null) _backSide.SetActive(false);

        if (_cardScaler != null)
        {
            _cardScaler.SetHomeRotation(Quaternion.identity);
            _cardScaler.RotationOverride = false;
        }
    }

    private IEnumerator FlipRoutine(bool targetFlipped)
    {
        _isAnimating = true;
        if (_cardScaler != null) _cardScaler.RotationOverride = true;

        // If flipping to back, update the description
        if (targetFlipped && _cardInfo != null && _cardInfo.CharData != null && _descriptionLabel != null)
        {
            _descriptionLabel.SetKey(_cardInfo.CharData.CharacterDescription);
            
            if (_descriptionLabel.Text != null)
            {
                // Не міняємо шрифт, просто просимо TMPro оновити меш для розрахунків
                _descriptionLabel.Text.ForceMeshUpdate();
                
                RectTransform labelRect = _descriptionLabel.GetComponent<RectTransform>();
                // Рахуємо висоту саме під поточну ширину лейбла
                float textHeight = _descriptionLabel.Text.GetPreferredValues(labelRect.rect.width, 0).y;
                
                // Чітко ставимо висоту лейбла
                labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
                
                // Пхаємо батьківський об'єкт (Content), щоб він перерахував свій розмір
                if (labelRect.parent is RectTransform contentRect)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                    Canvas.ForceUpdateCanvases();

                    // Скидаємо скролл в самий верх
                    ScrollRect scroll = _descriptionScrollRect != null ? _descriptionScrollRect : _descriptionLabel.GetComponentInParent<ScrollRect>();
                    if (scroll != null)
                    {
                        scroll.verticalNormalizedPosition = 1f;
                    }
                }
            }
        }

        float elapsed = 0;
        Quaternion startRotation = transform.localRotation;
        
        // Target state will be set at the end
        Quaternion endRotation = Quaternion.Euler(0, targetFlipped ? 180 : 0, 0);

        bool contentSwapped = false;

        while (elapsed < _flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _flipDuration;
            float curveT = _flipCurve.Evaluate(t);

            transform.localRotation = Quaternion.Lerp(startRotation, endRotation, curveT);

            // Swap visual content at 90 degrees
            if (!contentSwapped && t >= 0.5f)
            {
                contentSwapped = true;
                if (_frontSide != null) _frontSide.SetActive(!targetFlipped);
                if (_backSide != null) _backSide.SetActive(targetFlipped);
            }

            yield return null;
        }

        transform.localRotation = endRotation;
        _isFlipped = targetFlipped;
        
        if (_cardScaler != null)
        {
            _cardScaler.SetHomeRotation(endRotation);
            _cardScaler.RotationOverride = false;
        }

        _isAnimating = false;
    }
}
