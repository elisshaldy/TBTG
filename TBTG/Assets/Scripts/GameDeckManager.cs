using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameDeckManager : MonoBehaviour
{
    [Header("Deck Data")]
    [Tooltip("������� ������ (���) ��� ��������� ����.")]
    public MasterDeckData MasterDeck;

    [Tooltip("���� ������ 1, ���� �� �������� ������ �����.")]
    public PlayerHandData Player1Hand;

    [Tooltip("���� ������ 2, ���� �� �������� ������ �����.")]
    public PlayerHandData Player2Hand;

    [Header("Draft UI")]
    public GameObject ConfirmSelectionButton;
    public int CardsToSelect = 8;
    public int CardsToShow = 10;

    [Header("UI References")]
    [Tooltip("RawImage �� �� ���� �������� �� ��� ������")]
    public RawImage GameFieldRawImage;

    [Tooltip("CanvasGroup ��� �������� �������������� �񳺿 ����� ������")]
    public CanvasGroup DraftCanvasGroup;

    [Header("Drag & Drop References")]
    public PairFormationManager PairFormationManager;

    [Header("Manager References")]
    public PlayerCardManager CardManager;
    [Tooltip("��������, ���� �������� �������� ���� - ������ ���.")]
    public TraitPurchaseManager TraitPurchaseManager;

    private GameManager _gameManager;
    private List<CardSelectionHandler> _activeDraftCards = new List<CardSelectionHandler>();
    private List<(CharacterData data, CardSelectionHandler handler, SelectionMode mode)> _selectedCardsInfo =
        new List<(CharacterData data, CardSelectionHandler handler, SelectionMode mode)>();
    private List<CharacterData> _selectedCharacters = new List<CharacterData>();
    private bool _isDraftPhaseActive = false;
    private PlayerHandData _activePlayerHand;
    private int _activePlayerID = 1;

    void Awake()
    {
        // �������������, �� �� ��������� ��������� �����������
        Assert.IsNotNull(MasterDeck, "MasterDeckData �� ���������� � GameDeckManager.");
        Assert.IsNotNull(Player1Hand, "Player1HandData �� ���������� � GameDeckManager.");
        Assert.IsNotNull(Player2Hand, "Player2HandData �� ���������� � GameDeckManager.");
        Assert.IsNotNull(CardManager, "CardManager �� ���������� � GameDeckManager.");
        Assert.IsNotNull(TraitPurchaseManager, "TraitPurchaseManager �� ���������� � GameDeckManager.");
        Assert.IsNotNull(PairFormationManager, "PairFormationManager �� ���������� � GameDeckManager.");

        // ��������� GameManager, ���� �� �� �����������
        if (_gameManager == null) _gameManager = FindObjectOfType<GameManager>();

        // Підписуємося на подію оновлення пар, щоб кнопка "Завершити вибір"
        // оновлювала свій стан одразу, коли всі 8 карток розкладені в пари.
        if (PairFormationManager != null)
        {
            PairFormationManager.OnPairsUpdated.AddListener(OnPairsUpdated);
        }

        if (ConfirmSelectionButton != null) ConfirmSelectionButton.SetActive(false);

        // �������� �������� RawImage
        SetGameFieldRawImageActive(false);
    }

    void Start()
    {
        // �������� ����� ��� ������� ������.
        StartDraftPhase();
    }

    /// <summary>
    /// ��������� ���������� RawImage �� ����� UI ��������
    /// </summary>
    private void SetGameFieldRawImageActive(bool isActive)
    {
        if (GameFieldRawImage != null)
        {
            GameFieldRawImage.gameObject.SetActive(isActive);
            Debug.Log($"GameFieldRawImage ����������: {isActive}");
        }

        // ��������� ����������� ��������������� �񳺿 ����� ������
        if (DraftCanvasGroup != null)
        {
            DraftCanvasGroup.interactable = isActive;
            DraftCanvasGroup.blocksRaycasts = isActive;
        }
    }

    // ----------------------------------------------------------------------
    // ���� ������ � DRAG & DROP
    // ----------------------------------------------------------------------

    public void StartDraftPhase()
    {
        // ��������� RawImage �� ������� ������
        SetGameFieldRawImageActive(false);

        // ������� ������� ���
        PairFormationManager.ResetAllPairs();

        // 1. ��������� ������������ ����������, ��� ���������� ������
        if (CardManager != null && CardManager.CardsContainer != null)
        {
            CardManager.CardsContainer.gameObject.SetActive(true);
        }

        // ���������, ��� ��� ����� (P1 ��� P2)
        PlayerHandData opponentHand = (_activePlayerID == 1) ? Player2Hand : Player1Hand;
        _activePlayerHand = (_activePlayerID == 1) ? Player1Hand : Player2Hand;

        // ������� �������� ������ ������
        _selectedCharacters.Clear();
        _selectedCardsInfo.Clear();
        _isDraftPhaseActive = true;

        _activePlayerHand.ClearHand();
        Debug.Log($"P{_activePlayerID}: Starting Draft Phase. Hand cleared.");

        // 1. ��������� ��� ���� ��� ������
        List<CharacterData> availablePool = MasterDeck.AllAvailableCharacters
            .Except(Player1Hand.SelectedCharacters)
            .Except(Player2Hand.SelectedCharacters)
            .Except(Player1Hand.DiscardedCharacters)
            .Except(Player2Hand.DiscardedCharacters)
            .ToList();

        // 2. ���������� �� ������� CardsToShow ����
        List<CharacterData> draftPool = availablePool
            .OrderBy(x => Random.value)
            .Take(CardsToShow)
            .ToList();

        // 3. ��������� ������ �� �����
        _activeDraftCards.Clear();
        _activeDraftCards = CardManager.LoadDraftCards(draftPool);

        // 4. ����������� drag & drop �������
        InitializeDragDropSystem(_activeDraftCards);

        UpdateConfirmButtonState();

        Debug.Log($"����� ���� ��� ������ {_activePlayerID} ���������. Drag & Drop ��������.");
    }

    /// <summary>
    /// ��������� drag & drop ������� ��� ������ ������
    /// </summary>
    private void InitializeDragDropSystem(List<CardSelectionHandler> draftHandlers)
    {
        foreach (var cardHandler in draftHandlers)
        {
            // ϳ������ �� ��䳿 ���� & ����
            cardHandler.OnCardBeginDrag += PairFormationManager.HandleCardBeginDrag;
            cardHandler.OnCardEndDrag += PairFormationManager.HandleCardEndDrag;
            cardHandler.OnCardDropped += PairFormationManager.HandleCardDropped;
            cardHandler.OnCardReturnedToDraft += PairFormationManager.HandleCardReturnedToDraft;
        }

        // �������� ���� & ����
        PairFormationManager.SetDragDropEnabled(true);

        // ϳ������ �� ��䳿 ���������� ���
        // ����� ������ ����� UnityEvent � PairFormationManager
    }

    /// <summary>
    /// ������� ���� ������ ������������ �� ����� ���������� ���
    /// </summary>
    private void UpdateConfirmButtonState()
    {
        if (ConfirmSelectionButton != null)
        {
            bool allPairsComplete = PairFormationManager.GetCompletedPairsCount() == 4;
            ConfirmSelectionButton.SetActive(allPairsComplete);

            if (allPairsComplete)
            {
                Debug.Log("All 4 pairs completed! Confirm button activated.");
            }
        }
    }

    /// <summary>
    /// ����������� ��� ���� ����� ��� (����� ���������� ����� UnityEvent)
    /// </summary>
    public void OnPairsUpdated()
    {
        UpdateConfirmButtonState();

        // ��������� ������ ������� ������ � ���
        UpdateSelectedCharactersFromPairs();
    }

    /// <summary>
    /// ������� ������ ������� ������ �� ����� ����������� ���
    /// </summary>
    private void UpdateSelectedCharactersFromPairs()
    {
        _selectedCharacters.Clear();
        _selectedCardsInfo.Clear();

        var formedPairs = PairFormationManager.GetFormedPairs();
        foreach (var pair in formedPairs)
        {
            if (pair.ActiveCharacter != null)
            {
                _selectedCharacters.Add(pair.ActiveCharacter);
                // ��������� handler ��� ������� ������
                var activeHandler = _activeDraftCards.FirstOrDefault(h => h.CardData == pair.ActiveCharacter);
                if (activeHandler != null)
                {
                    _selectedCardsInfo.Add((pair.ActiveCharacter, activeHandler, SelectionMode.Visible));
                }
            }

            if (pair.HiddenCharacter != null)
            {
                _selectedCharacters.Add(pair.HiddenCharacter);
                // ��������� handler ��� ��������� ������
                var hiddenHandler = _activeDraftCards.FirstOrDefault(h => h.CardData == pair.HiddenCharacter);
                if (hiddenHandler != null)
                {
                    _selectedCardsInfo.Add((pair.HiddenCharacter, hiddenHandler, SelectionMode.Hidden));
                }
            }
        }

        Debug.Log($"Updated selected characters: {_selectedCharacters.Count} cards from {formedPairs.Count} pairs");
    }

    /// <summary>
    /// ����������� ��� ���������� ������ "ϳ��������� ����".
    /// </summary>
    public void ConfirmSelection()
    {
        if (PairFormationManager.GetCompletedPairsCount() != 4)
        {
            Debug.LogWarning("�� �� ���� ����������! ������� 4 ������ ���.");
            return;
        }

        _isDraftPhaseActive = false;

        // Գ������� ����� ����
        FinalizeDraftPhase();

        if (_activePlayerID == 1)
        {
            // ���������� �� P2
            _activePlayerID = 2;
            Debug.Log("Draft P1 Completed. Initiating Draft for Player 2.");

            // ��������� RawImage ��������� ��� ������� ������
            StartDraftPhase();
        }
        else
        {
            // ������ ������ ��������� �����
            Debug.Log("Draft Phase Completed for both players.");

            // �Ѳ ����ֲ ��������� ����� - �²������ RAWIMAGE
            SetGameFieldRawImageActive(true);

            // 5. �������� ���� ����� ��� (Trait Purchase)
            StartTraitPurchasePhase();
        }
    }

    /// <summary>
    /// Գ����� ����� ���� �� ���� ���� ��� �������� ����
    /// </summary>
    private void FinalizeDraftPhase()
    {
        // �������� ���� & ����
        PairFormationManager.SetDragDropEnabled(false);

        // �������� ���������� ����
        var formedPairs = PairFormationManager.GetFormedPairs();

        // ��������� ������ ������� ������
        UpdateSelectedCharactersFromPairs();

        // --- ��ò��: ���������� ���������� ���� (�� ����� � �������) ---
        List<CharacterData> unselectedCharacters =
            _activeDraftCards
                .Select(csh => csh.CardData)
                .Except(_selectedCharacters)
                .ToList();
        // ------------------------------------------

        // 1. �������� ������ ����� � ������� ����
        _activePlayerHand.SelectedCharacters.Clear();
        _activePlayerHand.SelectedCharacters.AddRange(_selectedCharacters);

        // 2. �������� ��������� ����� (�� ����� �� ������ ��������)
        _activePlayerHand.DiscardedCharacters.Clear();
        _activePlayerHand.DiscardedCharacters.AddRange(unselectedCharacters);

        Debug.Log($"P{_activePlayerID}: Draft confirmed. {_selectedCharacters.Count} selected, {unselectedCharacters.Count} discarded.");

        // !!! ���в����� ����̲� ������ (Visible/Hidden) !!!
        _activePlayerHand.SetSelectionModes(_selectedCardsInfo.Select(i => (i.data, i.mode)).ToList());
        // ------------------------------------------

        // 3. ������� ����� �� UI ��������
        CardManager.ClearCards();
        if (CardManager.CardsContainer != null)
        {
            CardManager.CardsContainer.gameObject.SetActive(false);
        }

        if (ConfirmSelectionButton != null) ConfirmSelectionButton.SetActive(false);

        Debug.Log($"Draft phase for player {_activePlayerID} finalized successfully.");
    }

    // ----------------------------------------------------------------------
    // ���� ������� ���
    // ----------------------------------------------------------------------

    private void StartTraitPurchasePhase()
    {
        Debug.Log("Draft Phase Completed. Starting Trait Purchase Phase. RawImage ��������.");

        if (TraitPurchaseManager != null)
        {
            // �������� ����� ���� ��������� ��� ������� ����
            TraitPurchaseManager.StartPurchasePhase(Player1Hand, Player2Hand);
        }
        else
        {
            Debug.LogError("TraitPurchaseManager �� ����������. ��������� ������� �� �������� ����.");
        }
    }

    // ----------------------------------------------------------------------
    // �������² ���˲�Ͳ ������ ��� ��������
    // ----------------------------------------------------------------------

    /// <summary>
    /// �������� ����� ��� ����������� ��������/��������� RawImage
    /// </summary>
    public void SetRawImageActive(bool active)
    {
        SetGameFieldRawImageActive(active);
    }

    /// <summary>
    /// ��������� �� ��������� ����� ����
    /// </summary>
    public bool IsDraftPhaseComplete()
    {
        return _activePlayerID == 2 && !_isDraftPhaseActive;
    }

    /// <summary>
    /// �������� �������� ������� ������
    /// </summary>
    public (int currentPlayer, int completedPairs, int totalPairs) GetDraftProgress()
    {
        return (_activePlayerID, PairFormationManager.GetCompletedPairsCount(), 4);
    }

    /// <summary>
    /// ������� ����� ������� (��� �������� ���)
    /// </summary>
    public void ResetDraftSystem()
    {
        _activePlayerID = 1;
        _isDraftPhaseActive = false;
        _selectedCharacters.Clear();
        _selectedCardsInfo.Clear();
        _activeDraftCards.Clear();

        // ������� ���� �������
        Player1Hand.ClearHand();
        Player2Hand.ClearHand();

        // ������� ������� ���
        PairFormationManager.ResetAllPairs();

        // �������� RawImage ��� �������
        SetGameFieldRawImageActive(false);

        Debug.Log("Draft system reset complete.");
    }

    /// <summary>
    /// ��������� ��������� ����� ��������� ������ (��� ����������)
    /// </summary>
    public void ForceCompleteDraft()
    {
        if (_isDraftPhaseActive)
        {
            Debug.Log("Forcing draft completion...");
            ConfirmSelection();
        }
    }
}