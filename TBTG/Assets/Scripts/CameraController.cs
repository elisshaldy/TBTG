using UnityEngine;

// Скрипт для керування орбітальною камерою (обертання, зум).
public class CameraController : MonoBehaviour
{
    [Header("Target & Camera")]
    [Tooltip("Трансформ, навколо якого обертатиметься камера (поточна ціль).")]
    public Transform Target;

    [Tooltip("Камера, якою ми керуємо. Зазвичай це GameFieldCamera.")]
    public Camera GameCamera;

    [Tooltip("Render Texture, в яку рендерить камера. Потрібна для ручного очищення.")]
    public RenderTexture RenderTextureTarget; // !!! НОВЕ ПОЛЕ: ДЛЯ РУЧНОГО ОЧИЩЕННЯ !!!


    [Header("Movement & Zoom Settings")]
    public float RotationSpeed = 5f;        // Швидкість обертання
    public float ZoomSpeed = 50f;           // Швидкість зуму (коліщатко)
    public float SmoothTime = 0.2f;         // Час для плавного руху камери до нової цілі (новий параметр)
    public float MinZoomDistance = 2f;      // Мінімальна відстань до об'єкта
    public float MaxZoomDistance = 15f;     // Максимальна відстань до об'єкта

    private float _distance;
    private float _currentX;
    private float _currentY;

    // Нове поле: Зберігає поточну згладжену позицію, навколо якої ми обертаємось.
    private Vector3 _currentFocusPosition;
    // Вектор швидкості для плавного руху (потрібен для SmoothDamp)
    private Vector3 _velocity = Vector3.zero;

    private const float Y_ANGLE_MIN = 10.0f;
    private const float Y_ANGLE_MAX = 80.0f; // Камера не дивитиметься повністю знизу/зверху

    // НОВЕ ПОЛЕ: Фіксована початкова відстань
    private const float DEFAULT_START_DISTANCE = 8f;

    // !!! НОВЕ: Колір для ручного очищення (Прозорий чорний) !!!
    private Color _clearColor = new Color(0f, 0f, 0f, 0f);


    void Start()
    {
        // Перевірка і призначення GameCamera
        if (GameCamera == null)
        {
            GameCamera = GetComponent<Camera>();
            if (GameCamera == null)
            {
                Debug.LogError("GameCamera не призначено! Призначте GameFieldCamera.");
                return;
            }
        }

        // Перевірка, чи призначена Target Texture в Інспекторі (для безпеки)
        if (GameCamera.targetTexture == null && RenderTextureTarget == null)
        {
            Debug.LogWarning("Render Texture не призначена ні камері, ні в полі RenderTextureTarget. Ручне очищення буде недоступне.");
        }
        else if (GameCamera.targetTexture != RenderTextureTarget)
        {
            // Якщо поле в скрипті відрізняється від поля в камері, синхронізуємо їх.
            RenderTextureTarget = GameCamera.targetTexture;
        }

        // Перевірка Target
        if (Target == null)
        {
            Debug.LogError("Target (Центр поля) не призначено! Призначте FieldCenterPoint як початкову ціль.");
            return;
        }

        // КРОК 1. Ініціалізація фокусної позиції 
        _currentFocusPosition = Target.position;

        // КРОК 2. ПРИМУСОВЕ СКИДАННЯ ПОЧАТКОВОЇ ПОЗИЦІЇ КАМЕРИ
        // Якщо камера знаходиться десь далеко, ми пересуваємо її на безпечну початкову відстань, 
        // щоб уникнути стрибка на першому кадрі.
        Vector3 initialDirection = GameCamera.transform.position - Target.position;
        _distance = initialDirection.magnitude;

        if (_distance < MinZoomDistance || _distance > MaxZoomDistance)
        {
            // Якщо початкова відстань не в межах розумного, використовуємо фіксоване значення
            _distance = DEFAULT_START_DISTANCE;

            // Скидаємо позицію камери, щоб вона була на DEFAULT_START_DISTANCE від цілі
            // ініціалізуємо її як би з початкової ротації
            GameCamera.transform.position = Target.position - (Quaternion.Euler(_currentY, _currentX, 0) * Vector3.forward * _distance);

            Debug.LogWarning($"Початкова відстань камери була за межами лімітів. Встановлено початкову відстань: {_distance}");
        }


        // Ініціалізація початкових значень кутів
        // Отримуємо початкові кути Ейлера (для коректного старту)
        Quaternion rotation = GameCamera.transform.rotation;
        _currentX = rotation.eulerAngles.y;
        _currentY = rotation.eulerAngles.x;

        // Встановлюємо початкове положення камери
        RotateAndPositionCamera(_currentFocusPosition);
    }

    void Update()
    {
        if (Target == null || GameCamera == null) return;

        // 1. ОБЕРТАННЯ КАМЕРИ (Права кнопка миші)
        if (Input.GetMouseButton(1)) // 1 - Права кнопка миші
        {
            // Якщо обертаємо, ми зупиняємо будь-яке автоматичне плавне переміщення до нової цілі.
            _velocity = Vector3.zero;

            _currentX += Input.GetAxis("Mouse X") * RotationSpeed;
            _currentY -= Input.GetAxis("Mouse Y") * RotationSpeed;

            // Обмежуємо обертання по вертикалі
            _currentY = Mathf.Clamp(_currentY, Y_ANGLE_MIN, Y_ANGLE_MAX);
        }

        // 2. ЗУМ (Коліщатко миші)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            // Оновлюємо дистанцію на основі прокрутки
            _distance -= scrollInput * ZoomSpeed * Time.deltaTime;
            _distance = Mathf.Clamp(_distance, MinZoomDistance, MaxZoomDistance);
        }
    }

    // LateUpdate ідеально підходить для логіки камери
    void LateUpdate()
    {
        if (Target != null && GameCamera != null)
        {
            // Ручне очищення RT перед рендерингом (на випадок, якщо Clear Flags не спрацювали)
            // Використовуємо OnPreRender, який ідеально підходить для цього (див. нижче)

            // КОРЕКТНЕ ВИПРАВЛЕННЯ: Плавно переміщуємо _currentFocusPosition 
            // до Target.position. Це усуває стрибок.
            _currentFocusPosition = Vector3.SmoothDamp(
                _currentFocusPosition, // Починаємо з поточної фокусної позиції
                Target.position,
                ref _velocity,
                SmoothTime
            );

            RotateAndPositionCamera(_currentFocusPosition);
        }
    }

    // !!! КРИТИЧНЕ ВИПРАВЛЕННЯ ДЛЯ ПІСЛЯОБРАЗІВ !!!
    // Цей метод викликається Unity безпосередньо перед тим, як камера починає рендерити.
    private void OnPreRender()
    {
        if (RenderTextureTarget != null && GameCamera.targetTexture != null && GameCamera.clearFlags != CameraClearFlags.SolidColor)
        {
            // Якщо Clear Flags не спрацювали, примусово очищуємо RT

            // 1. Встановлюємо Render Texture як поточну ціль рендерингу
            Graphics.SetRenderTarget(RenderTextureTarget);

            // 2. Очищуємо колірний буфер (true) і буфер глибини (true) заданим кольором
            // Це змушує RT очищатися, навіть якщо налаштування Clear Flags ігноруються через якісь шейдери/ефекти.
            GL.Clear(true, true, _clearColor);

            // Повертаємо ціль рендерингу на default (екран)
            Graphics.SetRenderTarget(null);
        }
        else if (GameCamera.targetTexture != null && GameCamera.clearFlags == CameraClearFlags.SolidColor)
        {
            // Якщо камера налаштована на Solid Color, ми покладаємося на вбудовану логіку Unity
            // і не робимо ручного очищення, щоб уникнути конфліктів.
        }
    }


    // ПУБЛІЧНИЙ МЕТОД: Зміна цілі камери (Центр поля або персонаж)
    // Викликається з іншого скрипта, коли потрібно змінити фокус.
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogWarning("Спроба встановити ціль камери на NULL.");
            return;
        }
        // Встановлюємо нову ціль. LateUpdate автоматично почне плавний рух.
        Target = newTarget;
    }


    private void RotateAndPositionCamera(Vector3 targetPosition)
    {
        // Розраховуємо кінцеву ротацію
        Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);

        // Розраховуємо кінцеву позицію камери відносно (плавно зміщеної) цілі
        Vector3 position = targetPosition - (rotation * Vector3.forward * _distance);

        // Застосовуємо ротацію та позицію
        GameCamera.transform.rotation = rotation;
        GameCamera.transform.position = position;
    }
}