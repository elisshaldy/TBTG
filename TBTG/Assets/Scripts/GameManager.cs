// GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static GameManager Instance { get; private set; }

    // --- DEPENDENCIES ---
    [Header("Dependencies")]
    public GridManager GridManager;
    public InitiativeManager InitiativeManager;

    // --- PLAYERS ---
    [Header("Players")]
    public PlayerController Player1;
    public PlayerController Player2;

    // --- UI REFERENCES ---
    [Tooltip("Панель UI, що затемнює екран під час передачі ходу (для Hot-Seat).")]
    public GameObject HandoverPanel;

    // --- GAME STATE ---
    [Header("Game State")]
    public int CurrentRound = 0;
    private PlayerController _activePlayer;

    // --- PLACEMENT STATE ---
    [Header("Placement State")]
    private Queue<PlayerController> _placementOrder;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Пошук залежностей, якщо не призначені
        if (GridManager == null) GridManager = FindObjectOfType<GridManager>();
        if (InitiativeManager == null) InitiativeManager = FindObjectOfType<InitiativeManager>();
    }

    void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Ініціалізація контролерів гравців
        Player1.Initialize(1, this);
        Player2.Initialize(2, this);

        // Переконайтеся, що панель прихована при старті
        if (HandoverPanel != null) HandoverPanel.SetActive(false);

        Debug.Log("Game Initialized. Starting Draft Phase.");
        // Тут має бути виклик StartDraftPhase() з GameDeckManager

        // !!! ДЛЯ ТЕСТУВАННЯ: МОЖНА ЗАПУСТИТИ PLACEMENT PHASE ВІДРАЗУ !!!
        // StartPlacementPhase();
    }

    // ----------------------------------------------------------------------
    // ФАЗА 1: Розміщення (Placement)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Починає фазу розміщення персонажів, визначаючи чергу.
    /// </summary>
    public void StartPlacementPhase()
    {
        CurrentRound = 1;
        _placementOrder = new Queue<PlayerController>(new[] { Player1, Player2 }); // По черзі

        // Приклад: Визначення зон розміщення (Тут мають бути реальні координати)
        List<Vector2Int> zone1 = new List<Vector2Int>();
        List<Vector2Int> zone2 = new List<Vector2Int>();

        // Встановлюємо зони в PlayerController (ТЕПЕР ЦЕ ПРАЦЮЄ!)
        Player1.SetPlacementZone(zone1);
        Player2.SetPlacementZone(zone2);

        StartNextPlacementTurn();
    }

    /// <summary>
    /// Запускає хід розміщення для наступного гравця.
    /// </summary>
    public void StartNextPlacementTurn() // Зроблено публічним, щоб викликати з PlayerController
    {
        if (_placementOrder.Count > 0)
        {
            PlayerController nextPlayer = _placementOrder.Dequeue();

            // !!! ВИПРАВЛЕНО ВИКЛИК !!!
            nextPlayer.StartPlacement(); // Викликаємо StartPlacement без передачі зони
        }
        else
        {
            Debug.Log("Placement Phase finished. Starting Initiative Phase.");
            // StartInitiativePhase();
        }
    }

    /// <summary>
    /// Повертає зону розміщення для PlayerController (залежить від GridManager).
    /// Цей метод був залишений для логічної повноти, хоча зони вже встановлені в SetPlacementZone.
    /// </summary>
    public List<Vector2Int> GetPlayerPlacementZone(int playerID)
    {
        // Це спрощений код, який має бути реалізований на рівні GridManager
        if (playerID == 1)
        {
            // Повернути зону гравця 1
            return new List<Vector2Int> { /* ... */ };
        }
        else if (playerID == 2)
        {
            // Повернути зону гравця 2
            return new List<Vector2Int> { /* ... */ };
        }
        return new List<Vector2Int>();
    }

    // ----------------------------------------------------------------------
    // ФАЗА 3: Виконання Ходу (З МОДИФІКАЦІЯМИ ДЛЯ HOT-SEAT)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Викликається InitiativeManager, коли починається хід персонажа.
    /// Перед початком ходу викликає Handover.
    /// </summary>
    public void StartCharacterTurn(InitiativeToken token)
    {
        // Визначаємо активного гравця
        _activePlayer = (token.PlayerID == Player1.PlayerID) ? Player1 : Player2;

        Debug.Log($"Hot Seat: Passing control to Player {_activePlayer.PlayerID} to move {token.CharacterReference.Data.CharacterName}.");

        // !!! НОВЕ: ПОЧАТОК ЛОГІКИ ПЕРЕДАЧІ ХОДУ !!!
        StartHandover(_activePlayer, token.CharacterReference);
    }

    /// <summary>
    /// Починає фазу затемнення екрана для передачі пристрою.
    /// </summary>
    private void StartHandover(PlayerController nextPlayer, Character characterToMove)
    {
        if (HandoverPanel == null)
        {
            Debug.LogError("HandoverPanel не призначено! Запускаємо хід негайно.");
            nextPlayer.StartTurn(characterToMove);
            return;
        }

        // Відображаємо панель
        HandoverPanel.SetActive(true);

        // Знаходимо кнопку на панелі, щоб прив'язати до неї логіку
        Button continueButton = HandoverPanel.GetComponentInChildren<Button>();
        if (continueButton != null)
        {
            // Очищаємо старі слухачі та додаємо новий
            continueButton.onClick.RemoveAllListeners();
            // Використовуємо функцію-обгортку для передачі контексту
            continueButton.onClick.AddListener(() => CompleteHandover(nextPlayer, characterToMove));
        }
        else
        {
            Debug.LogError("На HandoverPanel відсутній компонент Button! Додайте його, щоб гравець міг продовжити.");
            // Якщо немає кнопки, ми не можемо автоматично продовжити. Хід заморозиться.
        }
    }

    /// <summary>
    /// Завершує фазу затемнення і починає хід гравця.
    /// </summary>
    public void CompleteHandover(PlayerController nextPlayer, Character characterToMove)
    {
        // Приховуємо панель
        if (HandoverPanel != null)
        {
            HandoverPanel.SetActive(false);

            // Очищаємо слухача кнопки
            Button continueButton = HandoverPanel.GetComponentInChildren<Button>();
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
            }
        }

        // Починаємо фактичний хід гравця
        nextPlayer.StartTurn(characterToMove);

        Debug.Log($"Handover completed. Player {nextPlayer.PlayerID} is now active.");
    }


    /// <summary>
    /// Викликається PlayerController, коли персонаж вичерпав свої дії.
    /// </summary>
    public void EndCharacterTurn()
    {
        // Повідомляємо InitiativeManager, що хід завершено
        InitiativeManager.CompleteTurn();
    }

    // ----------------------------------------------------------------------
    // ФАЗА 4: Кінець Раунду
    // ----------------------------------------------------------------------
    public void EndRound()
    {
        // Логіка кінця раунду:
        // 1. Обробка ефектів (Active Tiles)
        // 2. Оновлення колод руху (MovementDeckManager.ReplenishHand)
        // 3. Збільшення лічильника раундів
        CurrentRound++;
        Debug.Log($"Round {CurrentRound - 1} ended. Starting Round {CurrentRound} Initiative Phase.");

        // StartInitiativePhase();
    }
}