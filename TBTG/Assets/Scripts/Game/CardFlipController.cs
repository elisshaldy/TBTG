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

    private IEnumerator FlipRoutine(bool targetFlipped)
    {
        _isAnimating = true;
        if (_cardScaler != null) _cardScaler.RotationOverride = true;

        // If flipping to back, update the description
        if (targetFlipped && _cardInfo != null && _cardInfo.CharData != null && _descriptionLabel != null)
        {
            _descriptionLabel.SetKey(_cardInfo.CharData.CharacterDescription);
            
            // Рахуємо букви і підганяємо розмір
            string text = LocalizationManager.GetTranslation(_cardInfo.CharData.CharacterDescription);
            int count = text.Length;
            
            float size = 26f; // Дефолт
            if (count > 400) size = 12f;
            else if (count > 250) size = 15f;
            else if (count > 150) size = 18f;
            else if (count > 80) size = 22f;

            if (_descriptionLabel.Text != null)
                _descriptionLabel.Text.fontSize = size;
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
