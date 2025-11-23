// TraitPurchaseManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// Менеджер фази купівлі рис для персонажів.
/// Генерує 35 випадкових рис з правильним розподілом, управляє UP балансом, обробляє drag&drop.
/// </summary>
public class TraitPurchaseManager : MonoBehaviour
{
    public static TraitPurchaseManager Instance { get; private set; }

    [Header("Master Data")]
    [Tooltip("MasterDeckData, який містить всі доступні риси.")]
    public MasterDeckData MasterDeck;

    [Header("UI References")]
    [Tooltip("Контейнер для відображення 35 випадкових рис (Traits_Container_Panel).")]
    public Transform TraitsContainerPanel;

    [Tooltip("Префаб картки риси для drag&drop.")]
    public GameObject TraitCardPrefab;

    [Tooltip("Кнопка підтвердження вибору рис.")]
    public GameObject ConfirmTraitsButton;

    [Tooltip("Текст для відображення поточного UP балансу.")]
    public Text UPBalanceText;

    [Header("UP Economy Settings")]
    [Tooltip("Загальний пул UP на команду (24 за GDD).")]
    public int TotalTeamUP = 24;

    [Tooltip("Максимум UP на одного персонажа (5 за GDD).")]
    public int MaxUPPerCharacter = 5;

    [Header("Trait Pool Distribution (GDD)")]
    [Tooltip("Кількість рис за 5 UP в пулі (3 за GDD).")]
    public int Traits5UP = 3;

    [Tooltip("Кількість рис за 4 UP в пулі (6 за GDD).")]
    public int Traits4UP = 6;

    [Tooltip("Кількість рис за 3 UP в пулі (9 за GDD).")]
    public int Traits3UP = 9;

    [Tooltip("Кількість рис за 2 UP в пулі (12 за GDD).")]
    public int Traits2UP = 12;

    [Tooltip("Кількість рис за 1 UP в пулі (15 за GDD).")]
    public int Traits1UP = 15;

    // Стан фази купівлі
    private List<TraitData> _availableTraitPool = new List<TraitData>(); // 35 випадкових рис
    private List<TraitCardHandler> _spawnedTraitCards = new List<TraitCardHandler>();
    private Dictionary<CharacterData, int> _characterUPUsage = new Dictionary<CharacterData, int>(); // Скільки UP витрачено на кожного персонажа
    private int _totalUPUsed = 0;
    private PlayerHandData _currentPlayerHand;
    private int _currentPlayerID = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ----------------------------------------------------------------------
    // Генерація пулу рис та старт фази
    // ----------------------------------------------------------------------

    /// <summary>
    /// Запускає фазу купівлі рис для обох гравців.
    /// </summary>
    public void StartPurchasePhase(PlayerHandData player1Hand, PlayerHandData player2Hand)
    {
        Debug.Log("Game Phase: Trait Purchase Phase Initiated.");

        // Почнемо з гравця 1
        _currentPlayerID = 1;
        _currentPlayerHand = player1Hand;

        StartPlayerPurchasePhase(_currentPlayerHand);
    }

    /// <summary>
    /// Запускає фазу купівлі рис для конкретного гравця.
    /// </summary>
    private void StartPlayerPurchasePhase(PlayerHandData playerHand)
    {
        Debug.Log($"Starting trait purchase phase for Player {_currentPlayerID}");

        // Очищаємо попередній стан
        ClearTraitPurchaseState();

        // Генеруємо пул з 35 випадкових рис
        GenerateRandomTraitPool();

        // Відображаємо риси в UI
        DisplayTraitPool();

        // Ініціалізуємо UP баланс для всіх персонажів гравця
        InitializeUPUsage(playerHand);

        // Оновлюємо UI балансу
        UpdateUPBalanceUI();

        // Вмикаємо контейнер рис
        if (TraitsContainerPanel != null)
        {
            TraitsContainerPanel.gameObject.SetActive(true);
        }

        if (ConfirmTraitsButton != null)
        {
            ConfirmTraitsButton.SetActive(false); // Буде активуватись, коли всі риси розподілені
        }
    }

    /// <summary>
    /// Генерує пул з 35 випадкових рис з правильним розподілом за вартістю.
    /// </summary>
    private void GenerateRandomTraitPool()
    {
        _availableTraitPool.Clear();

        if (MasterDeck == null || MasterDeck.AllAvailableTraits == null || MasterDeck.AllAvailableTraits.Count == 0)
        {
            Debug.LogError("MasterDeck.AllAvailableTraits is empty! Cannot generate trait pool.");
            return;
        }

        // Групуємо риси за вартістю
        var traitsByCost = MasterDeck.AllAvailableTraits
            .Where(t => t != null)
            .GroupBy(t => t.Cost)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Генеруємо потрібну кількість рис для кожної вартості
        AddTraitsToPool(traitsByCost, 5, Traits5UP);
        AddTraitsToPool(traitsByCost, 4, Traits4UP);
        AddTraitsToPool(traitsByCost, 3, Traits3UP);
        AddTraitsToPool(traitsByCost, 2, Traits2UP);
        AddTraitsToPool(traitsByCost, 1, Traits1UP);

        // Перемішуємо пул
        _availableTraitPool = _availableTraitPool.OrderBy(x => Random.value).ToList();

        Debug.Log($"Generated trait pool: {_availableTraitPool.Count} traits " +
                  $"({Traits5UP} x 5UP, {Traits4UP} x 4UP, {Traits3UP} x 3UP, {Traits2UP} x 2UP, {Traits1UP} x 1UP)");
    }

    private void AddTraitsToPool(Dictionary<int, List<TraitData>> traitsByCost, int cost, int count)
    {
        if (!traitsByCost.ContainsKey(cost) || traitsByCost[cost].Count == 0)
        {
            Debug.LogWarning($"No traits with cost {cost} available in MasterDeck!");
            return;
        }

        var availableTraits = traitsByCost[cost];
        var selectedTraits = availableTraits.OrderBy(x => Random.value).Take(count).ToList();

        _availableTraitPool.AddRange(selectedTraits);
    }

    /// <summary>
    /// Відображає пул рис у UI контейнері.
    /// </summary>
    private void DisplayTraitPool()
    {
        if (TraitsContainerPanel == null || TraitCardPrefab == null)
        {
            Debug.LogError("TraitsContainerPanel or TraitCardPrefab is not assigned!");
            return;
        }

        // Очищаємо попередні картки
        ClearTraitCards();

        // Створюємо картки для кожної риси
        foreach (var trait in _availableTraitPool)
        {
            GameObject cardObj = Instantiate(TraitCardPrefab, TraitsContainerPanel);
            cardObj.name = $"TraitCard_{trait.TraitName}";

            TraitCardHandler handler = cardObj.GetComponent<TraitCardHandler>();
            if (handler == null)
            {
                Debug.LogError($"TraitCardPrefab does not have TraitCardHandler component!");
                Destroy(cardObj);
                continue;
            }

            handler.Initialize(trait);

            // Підписуємось на події
            handler.OnTraitDropped += OnTraitDropped;
            handler.OnTraitReturnedToPool += OnTraitReturnedToPool;

            _spawnedTraitCards.Add(handler);
        }

        Debug.Log($"Displayed {_spawnedTraitCards.Count} trait cards in UI");
    }

    /// <summary>
    /// Ініціалізує відстеження UP використання для всіх персонажів гравця.
    /// </summary>
    private void InitializeUPUsage(PlayerHandData playerHand)
    {
        _characterUPUsage.Clear();
        _totalUPUsed = 0;

        foreach (var character in playerHand.SelectedCharacters)
        {
            if (character != null)
            {
                // Рахуємо вже куплені риси (якщо є)
                int usedUP = character.PurchasedTraits?.Sum(t => t?.Cost ?? 0) ?? 0;
                _characterUPUsage[character] = usedUP;
                _totalUPUsed += usedUP;
            }
        }
    }

    // ----------------------------------------------------------------------
    // Обробка drag&drop та UP баланс
    // ----------------------------------------------------------------------

    /// <summary>
    /// Перевіряє, чи може персонаж дозволити собі цю рису.
    /// </summary>
    public bool CanAffordTrait(CharacterData character, TraitData trait)
    {
        if (character == null || trait == null) return false;

        int currentUP = _characterUPUsage.ContainsKey(character) ? _characterUPUsage[character] : 0;
        int traitCost = trait.Cost;

        // Перевірка: чи не перевищуємо ліміт на персонажа?
        if (currentUP + traitCost > MaxUPPerCharacter)
        {
            return false;
        }

        // Перевірка: чи не перевищуємо загальний пул команди?
        if (_totalUPUsed + traitCost > TotalTeamUP)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Обробляє подію, коли риса перетягнута на слот персонажа.
    /// </summary>
    private void OnTraitDropped(TraitCardHandler traitCard, TraitSlot slot)
    {
        if (slot == null || slot.AssociatedCharacter == null || traitCard.TraitData == null)
        {
            Debug.LogWarning("Invalid trait drop: slot or character is null");
            return;
        }

        CharacterData character = slot.AssociatedCharacter;
        TraitData trait = traitCard.TraitData;

        // Перевірка на дублікати
        if (character.PurchasedTraits.Contains(trait))
        {
            Debug.LogWarning($"Character {character.CharacterName} already has trait {trait.TraitName}");
            traitCard.ReturnToPool();
            return;
        }

        // Перевірка UP балансу
        if (!CanAffordTrait(character, trait))
        {
            Debug.LogWarning($"Cannot afford trait {trait.TraitName} for {character.CharacterName}");
            traitCard.ReturnToPool();
            return;
        }

        // Додаємо рису до персонажа
        if (character.PurchasedTraits == null)
        {
            character.PurchasedTraits = new List<TraitData>();
        }

        character.PurchasedTraits.Add(trait);

        // Оновлюємо UP баланс
        int currentUP = _characterUPUsage.ContainsKey(character) ? _characterUPUsage[character] : 0;
        _characterUPUsage[character] = currentUP + trait.Cost;
        _totalUPUsed += trait.Cost;

        UpdateUPBalanceUI();
        CheckPurchaseCompletion();

        Debug.Log($"Trait {trait.TraitName} ({trait.Cost} UP) assigned to {character.CharacterName}. " +
                  $"Remaining UP: {TotalTeamUP - _totalUPUsed}");
    }

    /// <summary>
    /// Обробляє подію, коли риса повернута зі слоту назад у пул.
    /// </summary>
    private void OnTraitReturnedToPool(TraitCardHandler traitCard)
    {
        // Знаходимо персонажа, у якого була ця риса, і видаляємо її
        foreach (var character in _currentPlayerHand.SelectedCharacters)
        {
            if (character != null && character.PurchasedTraits != null && character.PurchasedTraits.Contains(traitCard.TraitData))
            {
                character.PurchasedTraits.Remove(traitCard.TraitData);

                // Оновлюємо UP баланс
                int currentUP = _characterUPUsage.ContainsKey(character) ? _characterUPUsage[character] : 0;
                int traitCost = traitCard.TraitData.Cost;
                _characterUPUsage[character] = Mathf.Max(0, currentUP - traitCost);
                _totalUPUsed = Mathf.Max(0, _totalUPUsed - traitCost);

                UpdateUPBalanceUI();
                CheckPurchaseCompletion();

                Debug.Log($"Trait {traitCard.TraitData.TraitName} removed from {character.CharacterName}. " +
                          $"Remaining UP: {TotalTeamUP - _totalUPUsed}");
                break;
            }
        }
    }

    /// <summary>
    /// Оновлює UI відображення UP балансу.
    /// </summary>
    private void UpdateUPBalanceUI()
    {
        if (UPBalanceText != null)
        {
            UPBalanceText.text = $"UP: {_totalUPUsed} / {TotalTeamUP}";
        }
    }

    /// <summary>
    /// Перевіряє, чи завершена фаза купівлі (всі риси розподілені, або гравець готовий продовжити).
    /// </summary>
    private void CheckPurchaseCompletion()
    {
        // Можна додати логіку, коли активувати кнопку "Підтвердити"
        // Наприклад: коли витрачено мінімум X UP, або всі риси розподілені
        if (ConfirmTraitsButton != null)
        {
            // Поки що завжди активна (гравець може продовжити в будь-який момент)
            ConfirmTraitsButton.SetActive(true);
        }
    }

    /// <summary>
    /// Підтверджує вибір рис для поточного гравця і переходить до наступного.
    /// </summary>
    public void ConfirmTraitSelection()
    {
        Debug.Log($"Player {_currentPlayerID} confirmed trait selection. Total UP used: {_totalUPUsed}");

        // Переходимо до наступного гравця
        if (_currentPlayerID == 1)
        {
            _currentPlayerID = 2;
            _currentPlayerHand = GameDeckManager.Instance?.Player2Hand;
            if (_currentPlayerHand != null)
            {
                StartPlayerPurchasePhase(_currentPlayerHand);
            }
        }
        else
        {
            // Обидва гравці завершили вибір рис
            Debug.Log("Both players completed trait purchase phase. Moving to Placement Phase.");
            FinalizeTraitPurchasePhase();
        }
    }

    /// <summary>
    /// Завершує фазу купівлі рис і переходить до наступної фази гри.
    /// </summary>
    private void FinalizeTraitPurchasePhase()
    {
        ClearTraitCards();

        if (TraitsContainerPanel != null)
        {
            TraitsContainerPanel.gameObject.SetActive(false);
        }

        if (ConfirmTraitsButton != null)
        {
            ConfirmTraitsButton.SetActive(false);
        }

        // TODO: Викликати GameManager для переходу до Placement Phase
        // GameManager.Instance.StartPlacementPhase();
    }

    // ----------------------------------------------------------------------
    // Допоміжні методи
    // ----------------------------------------------------------------------

    private void ClearTraitPurchaseState()
    {
        ClearTraitCards();
        _availableTraitPool.Clear();
        _characterUPUsage.Clear();
        _totalUPUsed = 0;
    }

    private void ClearTraitCards()
    {
        foreach (var card in _spawnedTraitCards)
        {
            if (card != null)
            {
                //if (card.OnTraitDropped != null)
                    card.OnTraitDropped -= OnTraitDropped;
                //if (card.OnTraitReturnedToPool != null)
                    card.OnTraitReturnedToPool -= OnTraitReturnedToPool;

                Destroy(card.gameObject);
            }
        }
        _spawnedTraitCards.Clear();
    }
}
