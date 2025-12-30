// PairCardDragHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Обробляє перетягування пари персонажів з UI на поле для розміщення.
/// Картки в UI трохи зміщуються за курсором, але не переміщуються повністю.
/// Коли курсор покидає зону пари, картки повертаються назад.
/// На сцені з'являється 3D модель активної картки, яка слідує за курсором.
/// </summary>
public class PairCardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Pair Data")]
    [Tooltip("Пара персонажів, яку можна перетягувати")]
    public CharacterPair PairData;

    [Header("UI Card References")]
    [Tooltip("Контейнер з картками пари (має містити дві картки: Active та Hidden)")]
    public Transform PairContainer;

    [Header("Visual Feedback")]
    [Tooltip("Зміщення карток при початку перетягування (в локальних координатах UI)")]
    public Vector2 CardOffsetOnDrag = new Vector2(10f, 10f);

    [Tooltip("Швидкість повернення карток до початкової позиції")]
    public float ReturnSpeed = 10f;

    [Header("3D Model")]
    [Tooltip("Префаб 3D моделі персонажа для відображення під час перетягування")]
    public GameObject Character3DPrefab;

    [Tooltip("Висота 3D моделі над полем під час перетягування")]
    public float DragHeight = 2f;

    // Стан перетягування
    private bool _isDragging = false;
    private bool _isPointerOverPair = false;
    private Vector3[] _originalCardPositions = new Vector3[2];
    private RectTransform[] _cardTransforms = new RectTransform[2];
    private GameObject _dragged3DModel = null;
    private Camera _mainCamera;
    private PlacementInputHandler _placementHandler;

    // Події
    public System.Action<PairCardDragHandler, CharacterPair> OnPairBeginDrag;
    public System.Action<PairCardDragHandler> OnPairEndDrag;
    public System.Action<PairCardDragHandler, Vector2Int> OnPairDroppedOnTile;

    void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            _mainCamera = FindObjectOfType<Camera>();
        }

        _placementHandler = FindObjectOfType<PlacementInputHandler>();

        // Знаходимо картки в контейнері
        if (PairContainer != null)
        {
            int cardIndex = 0;
            foreach (Transform child in PairContainer)
            {
                if (cardIndex < 2)
                {
                    RectTransform rectTransform = child.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        _cardTransforms[cardIndex] = rectTransform;
                        _originalCardPositions[cardIndex] = rectTransform.localPosition;
                        cardIndex++;
                    }
                }
            }
        }
    }

    public void Initialize(CharacterPair pair)
    {
        PairData = pair;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (PairData == null || PairData.ActiveCharacter == null)
        {
            Debug.LogWarning("PairCardDragHandler: PairData or ActiveCharacter is null!");
            return;
        }

        _isDragging = true;
        _isPointerOverPair = true;

        // Зберігаємо початкові позиції карток
        for (int i = 0; i < _cardTransforms.Length; i++)
        {
            if (_cardTransforms[i] != null)
            {
                _originalCardPositions[i] = _cardTransforms[i].localPosition;
            }
        }

        // Створюємо 3D модель активної картки
        Create3DModelForDrag();

        // Повідомляємо PlacementInputHandler про початок перетягування
        if (_placementHandler != null)
        {
            _placementHandler.OnPairDragStarted(this, PairData);
        }

        OnPairBeginDrag?.Invoke(this, PairData);
        Debug.Log($"Started dragging pair: {PairData.ActiveCharacter.CharacterName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        // Оновлюємо позицію 3D моделі
        Update3DModelPosition(eventData);

        // Зміщуємо картки в UI, якщо курсор все ще над парою
        if (_isPointerOverPair)
        {
            UpdateCardPositions(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        _isDragging = false;
        _isPointerOverPair = false;

        // Повертаємо картки до початкових позицій
        ReturnCardsToOriginalPosition();

        // Перевіряємо, чи було розміщення на валідній клітинці
        bool placed = CheckPlacement(eventData);

        // Видаляємо 3D модель
        Destroy3DModel();

        // Повідомляємо PlacementInputHandler про завершення перетягування
        if (_placementHandler != null)
        {
            _placementHandler.OnPairDragEnded(this, placed);
        }

        OnPairEndDrag?.Invoke(this);
        Debug.Log($"Ended dragging pair: {PairData.ActiveCharacter.CharacterName}, placed: {placed}");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isDragging)
        {
            _isPointerOverPair = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isDragging)
        {
            _isPointerOverPair = false;
            // Повертаємо картки до початкових позицій, коли курсор покидає зону пари
            ReturnCardsToOriginalPosition();
        }
    }

    private void Create3DModelForDrag()
    {
        if (PairData?.ActiveCharacter == null)
        {
            Debug.LogWarning("PairCardDragHandler: ActiveCharacter is null. Cannot create 3D model.");
            return;
        }

        // Якщо префаб не встановлений, створюємо простий GameObject з Character компонентом
        if (Character3DPrefab == null)
        {
            _dragged3DModel = new GameObject($"DraggedCharacter_{PairData.ActiveCharacter.CharacterName}");
            Character characterComponent = _dragged3DModel.AddComponent<Character>();
            characterComponent.Initialize(PairData.ActiveCharacter);
            
            // Додаємо простий візуальний маркер (куб) для відображення
            GameObject visualMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualMarker.transform.SetParent(_dragged3DModel.transform);
            visualMarker.transform.localPosition = Vector3.zero;
            visualMarker.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            
            // Видаляємо колайдер, щоб не заважав raycast
            Collider collider = visualMarker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
        else
        {
            // Створюємо 3D модель з префаба
            _dragged3DModel = Instantiate(Character3DPrefab);
            _dragged3DModel.name = $"DraggedCharacter_{PairData.ActiveCharacter.CharacterName}";

            // Ініціалізуємо Character компонент, якщо він є
            Character characterComponent = _dragged3DModel.GetComponent<Character>();
            if (characterComponent != null)
            {
                characterComponent.Initialize(PairData.ActiveCharacter);
            }
        }

        // Встановлюємо початкову позицію (під курсором)
        Vector3 worldPos = GetWorldPositionFromScreen(Input.mousePosition);
        _dragged3DModel.transform.position = new Vector3(worldPos.x, DragHeight, worldPos.z);

        // Можна додати візуальні ефекти (наприклад, напівпрозорість)
        SetModelDragVisuals(_dragged3DModel, true);
    }

    private void Update3DModelPosition(PointerEventData eventData)
    {
        if (_dragged3DModel == null) return;

        Vector3 worldPos = GetWorldPositionFromScreen(eventData.position);
        _dragged3DModel.transform.position = new Vector3(worldPos.x, DragHeight, worldPos.z);
    }

    private void UpdateCardPositions(PointerEventData eventData)
    {
        // Зміщуємо картки трохи за курсором
        Vector2 offset = CardOffsetOnDrag;
        
        for (int i = 0; i < _cardTransforms.Length; i++)
        {
            if (_cardTransforms[i] != null)
            {
                Vector3 newPos = _originalCardPositions[i] + (Vector3)offset;
                _cardTransforms[i].localPosition = Vector3.Lerp(_cardTransforms[i].localPosition, newPos, Time.deltaTime * ReturnSpeed);
            }
        }
    }

    private void ReturnCardsToOriginalPosition()
    {
        for (int i = 0; i < _cardTransforms.Length; i++)
        {
            if (_cardTransforms[i] != null)
            {
                _cardTransforms[i].localPosition = Vector3.Lerp(_cardTransforms[i].localPosition, _originalCardPositions[i], Time.deltaTime * ReturnSpeed);
            }
        }
    }

    private bool CheckPlacement(PointerEventData eventData)
    {
        // Raycast для визначення, чи курсор над клітинкою
        Ray ray = _mainCamera.ScreenPointToRay(eventData.position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null)
            {
                // Перевіряємо, чи клітинка валідна для розміщення
                if (_placementHandler != null && _placementHandler.IsValidPlacementTile(tile))
                {
                    // Розміщуємо персонажа
                    OnPairDroppedOnTile?.Invoke(this, tile.GridCoordinates);
                    if (_placementHandler != null)
                    {
                        _placementHandler.HandlePairDroppedOnTile(this, tile.GridCoordinates);
                    }
                    return true;
                }
            }
        }

        return false;
    }

    private void Destroy3DModel()
    {
        if (_dragged3DModel != null)
        {
            Destroy(_dragged3DModel);
            _dragged3DModel = null;
        }
    }

    private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        // Проектуємо позицію курсора на площину поля (Y = 0)
        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        
        if (groundPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    private void SetModelDragVisuals(GameObject model, bool isDragging)
    {
        // Можна додати візуальні ефекти (наприклад, змінити альфа-канал матеріалу)
        // Поки що залишаємо базову реалізацію
    }

    void Update()
    {
        // Плавне повернення карток, якщо вони не на місці
        if (!_isDragging)
        {
            for (int i = 0; i < _cardTransforms.Length; i++)
            {
                if (_cardTransforms[i] != null)
                {
                    float distance = Vector3.Distance(_cardTransforms[i].localPosition, _originalCardPositions[i]);
                    if (distance > 0.1f)
                    {
                        _cardTransforms[i].localPosition = Vector3.Lerp(_cardTransforms[i].localPosition, _originalCardPositions[i], Time.deltaTime * ReturnSpeed);
                    }
                }
            }
        }
    }
}

