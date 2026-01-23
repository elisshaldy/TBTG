using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SmoothImageScaler : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float maxScaleMultiplier = 1.5f;
    [SerializeField] private float scaleDuration = 1.0f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Color Settings")]
    [SerializeField] private bool enableColorChange = false;
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private float colorChangeDuration = 1.0f;
    [SerializeField] private AnimationCurve colorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private float minRotationAngle = -30f;
    [SerializeField] private float maxRotationAngle = 30f;
    [SerializeField] private float rotationDuration = 1.0f;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool randomizeRotationDirection = true;

    [Header("References")]
    [SerializeField] private Image targetImage;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loopAnimation = true;

    private Vector3 originalScale;
    private Color originalColor;
    private Quaternion originalRotation;
    private Coroutine scaleCoroutine;
    private Coroutine colorCoroutine;
    private Coroutine rotationCoroutine;

    void Start()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage != null)
        {
            originalScale = targetImage.rectTransform.localScale;
            originalColor = targetImage.color;
            originalRotation = targetImage.rectTransform.localRotation;
        }
        else
        {
            Debug.LogError("No Image component found!");
            return;
        }

        if (playOnStart)
        {
            StartScaleAnimation();
            if (enableColorChange)
            {
                StartColorAnimation();
            }
            if (enableRotation)
            {
                StartRotationAnimation();
            }
        }
    }

    /// <summary>
    /// Почати анімацію масштабування
    /// </summary>
    public void StartScaleAnimation()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleAnimation());
    }

    /// <summary>
    /// Почати анімацію зміни кольору
    /// </summary>
    public void StartColorAnimation()
    {
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }
        colorCoroutine = StartCoroutine(ColorAnimation());
    }

    /// <summary>
    /// Почати анімацію обертання
    /// </summary>
    public void StartRotationAnimation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(RotationAnimation());
    }

    /// <summary>
    /// Зупинити всі анімації
    /// </summary>
    public void StopAllAnimations()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }

    /// <summary>
    /// Скинути до початкового стану
    /// </summary>
    public void ResetToOriginal()
    {
        StopAllAnimations();
        targetImage.rectTransform.localScale = originalScale;
        targetImage.color = originalColor;
        targetImage.rectTransform.localRotation = originalRotation;
    }

    /// <summary>
    /// Корин для плавного масштабування
    /// </summary>
    private IEnumerator ScaleAnimation()
    {
        do
        {
            // Збільшення масштабу
            yield return StartCoroutine(ScaleTo(originalScale * maxScaleMultiplier, scaleDuration));

            // Зменшення масштабу
            yield return StartCoroutine(ScaleTo(originalScale, scaleDuration));
        }
        while (loopAnimation);
    }

    /// <summary>
    /// Корин для плавної зміни масштабу
    /// </summary>
    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        Vector3 startScale = targetImage.rectTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float curveValue = scaleCurve.Evaluate(t);

            targetImage.rectTransform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            yield return null;
        }

        targetImage.rectTransform.localScale = targetScale;
    }

    /// <summary>
    /// Корин для плавної зміни кольору
    /// </summary>
    private IEnumerator ColorAnimation()
    {
        do
        {
            // Зміна до цільового кольору
            yield return StartCoroutine(ChangeColorTo(targetColor, colorChangeDuration));

            // Повернення до початкового кольору
            yield return StartCoroutine(ChangeColorTo(originalColor, colorChangeDuration));
        }
        while (loopAnimation && enableColorChange);
    }

    /// <summary>
    /// Корин для плавної зміни кольору
    /// </summary>
    private IEnumerator ChangeColorTo(Color targetColor, float duration)
    {
        Color startColor = targetImage.color;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float curveValue = colorCurve.Evaluate(t);

            targetImage.color = Color.Lerp(startColor, targetColor, curveValue);
            yield return null;
        }

        targetImage.color = targetColor;
    }

    /// <summary>
    /// Корин для плавного обертання
    /// </summary>
    private IEnumerator RotationAnimation()
    {
        do
        {
            // Генеруємо випадковий кут обертання
            float randomAngle = Random.Range(minRotationAngle, maxRotationAngle);

            // Випадково вибираємо напрямок обертання
            if (randomizeRotationDirection && Random.value > 0.5f)
            {
                randomAngle = -randomAngle;
            }

            // Створюємо кватерніон для цільового обертання
            Quaternion targetRotation = Quaternion.Euler(0, 0, randomAngle) * originalRotation;

            // Обертання до випадкового кута
            yield return StartCoroutine(RotateTo(targetRotation, rotationDuration));

            // Повернення до початкового обертання
            yield return StartCoroutine(RotateTo(originalRotation, rotationDuration));
        }
        while (loopAnimation && enableRotation);
    }

    /// <summary>
    /// Корин для плавного обертання до заданого кватерніону
    /// </summary>
    private IEnumerator RotateTo(Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = targetImage.rectTransform.localRotation;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float curveValue = rotationCurve.Evaluate(t);

            targetImage.rectTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);
            yield return null;
        }

        targetImage.rectTransform.localRotation = targetRotation;
    }

    /// <summary>
    /// Генерує випадковий кут обертання та обертає зображення (одноразово)
    /// </summary>
    public void RotateToRandomAngleOnce()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(RotateToRandomAngleOnceCoroutine());
    }

    /// <summary>
    /// Корин для одноразового обертання на випадковий кут
    /// </summary>
    private IEnumerator RotateToRandomAngleOnceCoroutine()
    {
        // Генеруємо випадковий кут обертання
        float randomAngle = Random.Range(minRotationAngle, maxRotationAngle);

        // Випадково вибираємо напрямок обертання
        if (randomizeRotationDirection && Random.value > 0.5f)
        {
            randomAngle = -randomAngle;
        }

        // Створюємо кватерніон для цільового обертання
        Quaternion targetRotation = Quaternion.Euler(0, 0, randomAngle) * originalRotation;

        // Обертання до випадкового кута
        yield return StartCoroutine(RotateTo(targetRotation, rotationDuration));

        // Повернення до початкового обертання
        yield return StartCoroutine(RotateTo(originalRotation, rotationDuration));

        rotationCoroutine = null;
    }

    /// <summary>
    /// Встановити множник масштабу
    /// </summary>
    public void SetMaxScaleMultiplier(float multiplier)
    {
        maxScaleMultiplier = Mathf.Max(1f, multiplier);
    }

    /// <summary>
    /// Встановити тривалість анімації масштабу
    /// </summary>
    public void SetScaleDuration(float duration)
    {
        scaleDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// Встановити цільовий колір
    /// </summary>
    public void SetTargetColor(Color newColor)
    {
        targetColor = newColor;
    }

    /// <summary>
    /// Встановити тривалість зміни кольору
    /// </summary>
    public void SetColorDuration(float duration)
    {
        colorChangeDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// Увімкнути/вимкнути зміну кольору
    /// </summary>
    public void SetColorChangeEnabled(bool enabled)
    {
        enableColorChange = enabled;
        if (enabled && colorCoroutine == null)
        {
            StartColorAnimation();
        }
        else if (!enabled && colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
            targetImage.color = originalColor;
        }
    }

    /// <summary>
    /// Увімкнути/вимкнути обертання
    /// </summary>
    public void SetRotationEnabled(bool enabled)
    {
        enableRotation = enabled;
        if (enabled && rotationCoroutine == null)
        {
            StartRotationAnimation();
        }
        else if (!enabled && rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
            targetImage.rectTransform.localRotation = originalRotation;
        }
    }

    /// <summary>
    /// Встановити діапазон кутів обертання
    /// </summary>
    public void SetRotationAngleRange(float minAngle, float maxAngle)
    {
        minRotationAngle = Mathf.Min(minAngle, maxAngle);
        maxRotationAngle = Mathf.Max(minAngle, maxAngle);
    }

    /// <summary>
    /// Встановити тривалість обертання
    /// </summary>
    public void SetRotationDuration(float duration)
    {
        rotationDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// Встановити випадковий напрямок обертання
    /// </summary>
    public void SetRandomizeRotationDirection(bool randomize)
    {
        randomizeRotationDirection = randomize;
    }

    /// <summary>
    /// Увімкнути/вимкнути циклічність анімації
    /// </summary>
    public void SetLoopAnimation(bool loop)
    {
        loopAnimation = loop;
    }

    /// <summary>
    /// Запустити всі анімації одночасно
    /// </summary>
    public void StartAllAnimations()
    {
        StartScaleAnimation();
        if (enableColorChange)
        {
            StartColorAnimation();
        }
        if (enableRotation)
        {
            StartRotationAnimation();
        }
    }

    void OnDestroy()
    {
        StopAllAnimations();
    }

    /// <summary>
    /// Тестовий метод для перевірки роботи (викликати через UI або іншим способом)
    /// </summary>
    [ContextMenu("Test Random Rotation")]
    public void TestRandomRotation()
    {
        RotateToRandomAngleOnce();
    }
}