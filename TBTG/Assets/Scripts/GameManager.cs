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
    
    [Tooltip("Посилання на MasterDeck для доступу до списку доступних ефектів клітинок.")]
    public MasterDeckData MasterDeck;

    // --- PLAYERS ---
    [Header("Players")]
    public PlayerController Player1;
    public PlayerController Player2;

    // --- UI REFERENCES ---
    [Tooltip("������ UI, �� �������� ����� �� ��� �������� ���� (��� Hot-Seat).")]
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
        // Singleton pattern: якщо вже є Instance, видаляємо поточний GameObject
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"GameManager instance already exists. Destroying duplicate: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        Debug.Log($"GameManager initialized on {gameObject.name}");

        // ����� �����������, ���� �� ����������
        if (GridManager == null) GridManager = FindObjectOfType<GridManager>();
        if (InitiativeManager == null) InitiativeManager = FindObjectOfType<InitiativeManager>();
    }

    void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        // ������������ ���������� �������
        Player1.Initialize(1, this);
        Player2.Initialize(2, this);

        // �������������, �� ������ ��������� ��� �����
        if (HandoverPanel != null) HandoverPanel.SetActive(false);

        Debug.Log("Game Initialized. Starting Draft Phase.");
        // ��� �� ���� ������ StartDraftPhase() � GameDeckManager

        // !!! ��� ����������: ����� ��������� PLACEMENT PHASE ²����� !!!
        // StartPlacementPhase();
    }

    // ----------------------------------------------------------------------
    // ���� 1: ��������� (Placement)
    // ----------------------------------------------------------------------

    /// <summary>
    /// ������ ���� ��������� ���������, ���������� �����.
    /// </summary>
    public void StartPlacementPhase()
    {
        CurrentRound = 1;
        _placementOrder = new Queue<PlayerController>(new[] { Player1, Player2 }); // �� ����

        // �������: ���������� ��� ��������� (��� ����� ���� ������� ����������)
        List<Vector2Int> zone1 = new List<Vector2Int>();
        List<Vector2Int> zone2 = new List<Vector2Int>();

        // ������������ ���� � PlayerController (����� �� ����ު!)
        Player1.SetPlacementZone(zone1);
        Player2.SetPlacementZone(zone2);

        StartNextPlacementTurn();
    }

    /// <summary>
    /// ������� ��� ��������� ��� ���������� ������.
    /// </summary>
    public void StartNextPlacementTurn() // �������� ��������, ��� ��������� � PlayerController
    {
        if (_placementOrder != null && _placementOrder.Count > 0)
        {
            PlayerController nextPlayer = _placementOrder.Dequeue();

            // !!! ���������� ������ !!!
            nextPlayer.StartPlacement(); // ��������� StartPlacement ��� �������� ����
        }
        else
        {
            Debug.Log("Placement Phase finished. Starting Initiative Phase.");
            // StartInitiativePhase();
        }
    }

    /// <summary>
    /// ������� ���� ��������� ��� PlayerController (�������� �� GridManager).
    /// ��� ����� ��� ��������� ��� ������ �������, ���� ���� ��� ����������� � SetPlacementZone.
    /// </summary>
    public List<Vector2Int> GetPlayerPlacementZone(int playerID)
    {
        // �� ��������� ���, ���� �� ���� ����������� �� ���� GridManager
        if (playerID == 1)
        {
            // ��������� ���� ������ 1
            List<Vector2Int> zone = new List<Vector2Int>();
            if (GridManager == null)
            {
                Debug.LogError("GridManager is null! Cannot calculate placement zone.");
                return zone;
            }
            int mapWidth = GridManager.MapWidth;
            int mapHeight = GridManager.MapHeight;
            // Гравець 1: трикутник у лівому нижньому куті
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col <= row; col++)
                {
                    Vector2Int coords = new Vector2Int(col, row);
                    if (coords.x >= 0 && coords.x < mapWidth && coords.y >= 0 && coords.y < mapHeight)
                    {
                        zone.Add(coords);
                    }
                }
            }
            Debug.Log($"Player {playerID} placement zone calculated: {zone.Count} tiles.");
            return zone;
        }
        else if (playerID == 2)
        {
            // ��������� ���� ������ 2
            List<Vector2Int> zone = new List<Vector2Int>();
            if (GridManager == null)
            {
                Debug.LogError("GridManager is null! Cannot calculate placement zone.");
                return zone;
            }
            int mapWidth = GridManager.MapWidth;
            int mapHeight = GridManager.MapHeight;
            // Гравець 2: трикутник у правому верхньому куті
            // Трикутник зі стороною 4: 4 клітинки на рядку mapHeight-4, 3 на mapHeight-3, 2 на mapHeight-2, 1 на mapHeight-1
            int startRow = mapHeight - 4;
            int startCol = mapWidth - 4;
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col <= (3 - row); col++)
                {
                    Vector2Int coords = new Vector2Int(startCol + col, startRow + row);
                    if (coords.x >= 0 && coords.x < mapWidth && coords.y >= 0 && coords.y < mapHeight)
                    {
                        zone.Add(coords);
                    }
                }
            }
            Debug.Log($"Player {playerID} placement zone calculated: {zone.Count} tiles.");
            return zone;
        }
        return new List<Vector2Int>();
    }

    /// <summary>
    /// Викликається PlayerController, коли гравець завершив розміщення всіх своїх персонажів.
    /// </summary>
    public void OnPlayerPlacementCompleted(int playerID)
    {
        Debug.Log($"Player {playerID} completed placement. Checking if all players are done...");

        bool player1Done = false;
        bool player2Done = false;

        if (Player1 != null && Player1.ActiveCharacters != null && Player1.CharactersToPlaceList != null)
        {
            player1Done = Player1.ActiveCharacters.Count == Player1.CharactersToPlaceList.Count;
        }

        if (Player2 != null && Player2.ActiveCharacters != null && Player2.CharactersToPlaceList != null)
        {
            player2Done = Player2.ActiveCharacters.Count == Player2.CharactersToPlaceList.Count;
        }

        if (player1Done && player2Done)
        {
            Debug.Log("All players completed placement. Placement Phase finished. Starting Initiative Phase.");
            // TODO: StartInitiativePhase();
        }
        else
        {
            if (_placementOrder != null && _placementOrder.Count > 0)
            {
                StartNextPlacementTurn();
            }
        }
    }

    // ----------------------------------------------------------------------
    // ���� 3: ��������� ���� (� ����Բ��ֲ��� ��� HOT-SEAT)
    // ----------------------------------------------------------------------

    /// <summary>
    /// ����������� InitiativeManager, ���� ���������� ��� ���������.
    /// ����� �������� ���� ������� Handover.
    /// </summary>
    public void StartCharacterTurn(InitiativeToken token)
    {
        // ��������� ��������� ������
        _activePlayer = (token.PlayerID == Player1.PlayerID) ? Player1 : Player2;

        Debug.Log($"Hot Seat: Passing control to Player {_activePlayer.PlayerID} to move {token.CharacterReference.Data.CharacterName}.");

        // !!! ����: ������� ��ò�� ������ײ ���� !!!
        StartHandover(_activePlayer, token.CharacterReference);
    }

    /// <summary>
    /// ������ ���� ���������� ������ ��� �������� ��������.
    /// </summary>
    private void StartHandover(PlayerController nextPlayer, Character characterToMove)
    {
        if (HandoverPanel == null)
        {
            Debug.LogError("HandoverPanel �� ����������! ��������� ��� �������.");
            nextPlayer.StartTurn(characterToMove);
            return;
        }

        // ³��������� ������
        HandoverPanel.SetActive(true);

        // ��������� ������ �� �����, ��� ����'����� �� �� �����
        Button continueButton = HandoverPanel.GetComponentInChildren<Button>();
        if (continueButton != null)
        {
            // ������� ���� ������� �� ������ �����
            continueButton.onClick.RemoveAllListeners();
            // ������������� �������-�������� ��� �������� ���������
            continueButton.onClick.AddListener(() => CompleteHandover(nextPlayer, characterToMove));
        }
        else
        {
            Debug.LogError("�� HandoverPanel �������� ��������� Button! ������� ����, ��� ������� �� ����������.");
            // ���� ���� ������, �� �� ������ ����������� ����������. ճ� ������������.
        }
    }

    /// <summary>
    /// ������� ���� ���������� � ������ ��� ������.
    /// </summary>
    public void CompleteHandover(PlayerController nextPlayer, Character characterToMove)
    {
        // ��������� ������
        if (HandoverPanel != null)
        {
            HandoverPanel.SetActive(false);

            // ������� ������� ������
            Button continueButton = HandoverPanel.GetComponentInChildren<Button>();
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
            }
        }

        // �������� ��������� ��� ������
        nextPlayer.StartTurn(characterToMove);

        Debug.Log($"Handover completed. Player {nextPlayer.PlayerID} is now active.");
    }


    /// <summary>
    /// ����������� PlayerController, ���� �������� �������� ��� 䳿.
    /// </summary>
    public void EndCharacterTurn()
    {
        // ����������� InitiativeManager, �� ��� ���������
        InitiativeManager.CompleteTurn();
    }

    // ----------------------------------------------------------------------
    // ���� 4: ʳ���� ������
    // ----------------------------------------------------------------------
    public void EndRound()
    {
        // ����� ���� ������:
        // 1. ������� ������ (Active Tiles)
        // 2. ��������� ����� ���� (MovementDeckManager.ReplenishHand)
        // 3. ��������� ��������� ������

        // 1. Обробка ефектів Active Tiles, які лікують / погіршують стан персонажів
        if (GridManager != null)
        {
            foreach (var coords in GridManager.GetAllRegisteredCoords())
            {
                Tile tile = GridManager.GetTile(coords);
                if (tile == null || tile.Type != TileType.ActiveTile) continue;
                if (tile.Occupant == null || tile.CurrentEffect == null) continue;

                // Лікування / погіршення стану згідно з ефектом клітинки
                if (tile.CurrentEffect.HealOnRoundEnd)
                {
                    tile.Occupant.ApplyHealing(1);
                }

                if (tile.CurrentEffect.WorsenStateOnRoundEnd)
                {
                    tile.Occupant.ApplyDamage(1);
                }
            }

            // 1.5. GDD: Зникнення/поява Active Tiles в кінці раунду
            RegenerateActiveTiles();
        }

        // 2. Штраф за пасивність для кожного гравця (спрощена реалізація без вибору карти через UI):
        // якщо в кінці раунду карт у руці > 2, суперник випадково "забирає" одну картку
        // (якщо в нього є місце — в руку, інакше у відбій).
        if (Player1 != null && Player2 != null)
        {
            ApplyInactivityPenaltyBetweenPlayers(Player1, Player2);
            ApplyInactivityPenaltyBetweenPlayers(Player2, Player1);
        }

        // 3. Добір карт руху до повного ліміту (якщо потрібно)
        Player1?.DeckManager?.ReplenishHand();
        Player2?.DeckManager?.ReplenishHand();

        CurrentRound++;
        Debug.Log($"Round {CurrentRound - 1} ended. Starting Round {CurrentRound} Initiative Phase.");

        // StartInitiativePhase();
    }

    /// <summary>
    /// Застосувати штраф за пасивність: якщо у гравця-бенефіціара (offender) наприкінці раунду
    /// більше 2 карт руху в руці, суперник випадково забирає одну картку.
    /// </summary>
    private void ApplyInactivityPenaltyBetweenPlayers(PlayerController offender, PlayerController opponent)
    {
        if (offender == null || opponent == null) return;
        if (offender.DeckManager == null || opponent.DeckManager == null) return;

        var offenderDeck = offender.DeckManager;
        var opponentDeck = opponent.DeckManager;

        if (offenderDeck.CurrentHandCount > 2)
        {
            MovementCardData cardToLose = offenderDeck.GetRandomCardFromHand();
            if (cardToLose == null) return;

            offenderDeck.RemoveCardFromHand(cardToLose);
            opponentDeck.ReceivePenaltyCard(cardToLose);

            Debug.Log($"Inactivity penalty: Player {offender.PlayerID} loses card '{cardToLose.CardName}' to Player {opponent.PlayerID}.");
        }
    }

    // ----------------------------------------------------------------------
    // РЕГЕНЕРАЦІЯ АКТИВНИХ КЛІТИНОК (GDD)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Регенерує Active Tiles згідно з GDD:
    /// - Незайняті Active Tiles зникають, перетворюючись на Plain
    /// - Відповідна кількість Plain перетворюються на нові Active Tiles з випадковими ефектами
    /// - Нові Active Tiles не з'являються на крайніх лініях поля
    /// - Нові Active Tiles віддалені від інших активних клітинок
    /// </summary>
    private void RegenerateActiveTiles()
    {
        if (GridManager == null || MasterDeck == null) return;
        if (MasterDeck.AllAvailableTileEffects == null || MasterDeck.AllAvailableTileEffects.Count == 0)
        {
            Debug.LogWarning("MasterDeck.AllAvailableTileEffects is empty! Cannot regenerate Active Tiles.");
            return;
        }

        List<Vector2Int> unoccupiedActiveTiles = new List<Vector2Int>();
        List<Vector2Int> occupiedActiveTiles = new List<Vector2Int>();

        // 1. Знаходимо всі Active Tiles та розділяємо їх на зайняті/незайняті
        foreach (var coords in GridManager.GetAllRegisteredCoords())
        {
            Tile tile = GridManager.GetTile(coords);
            if (tile == null || tile.Type != TileType.ActiveTile) continue;

            if (tile.IsOccupied)
            {
                occupiedActiveTiles.Add(coords);
            }
            else
            {
                unoccupiedActiveTiles.Add(coords);
            }
        }

        int tilesToRemove = unoccupiedActiveTiles.Count;
        Debug.Log($"Regenerating Active Tiles: {tilesToRemove} unoccupied tiles will be removed, {occupiedActiveTiles.Count} occupied tiles will remain.");

        // 2. Перетворюємо незайняті Active Tiles на Plain
        foreach (var coords in unoccupiedActiveTiles)
        {
            Tile tile = GridManager.GetTile(coords);
            if (tile != null)
            {
                tile.Type = TileType.Plain;
                tile.CurrentEffect = null;
                Debug.Log($"Active Tile at {coords} converted to Plain (unoccupied).");
            }
        }

        // 3. Знаходимо Plain клітинки, які можуть стати Active Tiles
        List<Vector2Int> candidatePlainTiles = new List<Vector2Int>();
        foreach (var coords in GridManager.GetAllRegisteredCoords())
        {
            Tile tile = GridManager.GetTile(coords);
            if (tile == null || tile.Type != TileType.Plain) continue;
            if (tile.IsOccupied) continue; // Не зайняті Plain

            // Перевірка: не на крайніх лініях поля (GDD: максимально віддалено від країв)
            if (coords.x <= 0 || coords.x >= GridManager.MapWidth - 1 ||
                coords.y <= 0 || coords.y >= GridManager.MapHeight - 1)
            {
                continue;
            }

            // Перевірка: віддаленість від інших Active Tiles (мінімум 2 клітинки)
            bool tooCloseToActive = false;
            foreach (var activeCoords in occupiedActiveTiles)
            {
                int distance = Mathf.Abs(coords.x - activeCoords.x) + Mathf.Abs(coords.y - activeCoords.y);
                if (distance < 2) // Мінімальна відстань 2 клітинки
                {
                    tooCloseToActive = true;
                    break;
                }
            }

            if (!tooCloseToActive)
            {
                candidatePlainTiles.Add(coords);
            }
        }

        // 4. Випадково обираємо Plain клітинки для перетворення на Active Tiles
        int tilesToCreate = Mathf.Min(tilesToRemove, candidatePlainTiles.Count);
        if (tilesToCreate > 0)
        {
            // Перемішуємо кандидатів
            for (int i = 0; i < candidatePlainTiles.Count; i++)
            {
                int randomIndex = Random.Range(i, candidatePlainTiles.Count);
                var temp = candidatePlainTiles[i];
                candidatePlainTiles[i] = candidatePlainTiles[randomIndex];
                candidatePlainTiles[randomIndex] = temp;
            }

            // Перетворюємо перші tilesToCreate клітинок на Active Tiles
            for (int i = 0; i < tilesToCreate; i++)
            {
                Vector2Int coords = candidatePlainTiles[i];
                Tile tile = GridManager.GetTile(coords);
                if (tile == null) continue;

                // Випадково обираємо ефект з доступних
                TileEffectData randomEffect = MasterDeck.AllAvailableTileEffects[Random.Range(0, MasterDeck.AllAvailableTileEffects.Count)];
                
                tile.Type = TileType.ActiveTile;
                tile.CurrentEffect = randomEffect;

                Debug.Log($"Plain tile at {coords} converted to Active Tile with effect: {randomEffect.EffectName}");
            }
        }
        else if (tilesToRemove > 0)
        {
            Debug.LogWarning($"Could not regenerate {tilesToRemove} Active Tiles: not enough suitable Plain tiles available.");
        }
    }

    // ----------------------------------------------------------------------
    // Допоміжні методи для PairSystem
    // ----------------------------------------------------------------------

    /// <summary>
    /// Отримує всіх активних персонажів з поля (з обох гравців).
    /// </summary>
    public List<Character> GetAllActiveCharacters()
    {
        List<Character> allActive = new List<Character>();
        
        if (Player1 != null && Player1.ActiveCharacters != null)
        {
            allActive.AddRange(Player1.ActiveCharacters);
        }
        
        if (Player2 != null && Player2.ActiveCharacters != null)
        {
            allActive.AddRange(Player2.ActiveCharacters);
        }

        return allActive;
    }

    /// <summary>
    /// Знаходить неактивного (резервного) персонажа за CharacterData.
    /// Шукає серед неактивних персонажів гравців (які ще не розміщені на полі).
    /// </summary>
    public Character GetInactiveCharacter(CharacterData characterData)
    {
        if (characterData == null) return null;

        // Шукаємо серед CharactersToPlaceList обох гравців
        if (Player1 != null && Player1.CharactersToPlaceList != null)
        {
            foreach (var character in Player1.CharactersToPlaceList)
            {
                if (character != null && character.Data == characterData && !character.gameObject.activeInHierarchy)
                {
                    return character;
                }
            }
        }

        if (Player2 != null && Player2.CharactersToPlaceList != null)
        {
            foreach (var character in Player2.CharactersToPlaceList)
            {
                if (character != null && character.Data == characterData && !character.gameObject.activeInHierarchy)
                {
                    return character;
                }
            }
        }

        // Якщо не знайдено в CharactersToPlaceList, шукаємо серед неактивних на сцені
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (var character in allCharacters)
        {
            if (character.Data == characterData && !character.gameObject.activeInHierarchy)
            {
                return character;
            }
        }

        return null;
    }

    /// <summary>
    /// Визначає PlayerID гравця, якому належить персонаж.
    /// </summary>
    public int GetPlayerID(Character character)
    {
        if (character == null) return 0;

        if (Player1 != null && Player1.ActiveCharacters != null && Player1.ActiveCharacters.Contains(character))
        {
            return Player1.PlayerID;
        }

        if (Player2 != null && Player2.ActiveCharacters != null && Player2.ActiveCharacters.Contains(character))
        {
            return Player2.PlayerID;
        }

        // Якщо не знайдено в ActiveCharacters, перевіряємо CharactersToPlaceList
        if (Player1 != null && Player1.CharactersToPlaceList != null && Player1.CharactersToPlaceList.Contains(character))
        {
            return Player1.PlayerID;
        }

        if (Player2 != null && Player2.CharactersToPlaceList != null && Player2.CharactersToPlaceList.Contains(character))
        {
            return Player2.PlayerID;
        }

        return 0;
    }

    /// <summary>
    /// Оновлює списки персонажів у PlayerController після заміни пари.
    /// </summary>
    public void UpdatePlayerCharacterLists(int playerID, Character oldActiveCharacter, Character newActiveCharacter)
    {
        PlayerController player = (playerID == 1) ? Player1 : Player2;
        if (player == null) return;

        // Видаляємо старого активного зі списку
        if (player.ActiveCharacters != null && oldActiveCharacter != null)
        {
            player.ActiveCharacters.Remove(oldActiveCharacter);
        }

        // Додаємо нового активного до списку
        if (player.ActiveCharacters != null && newActiveCharacter != null)
        {
            if (!player.ActiveCharacters.Contains(newActiveCharacter))
            {
                player.ActiveCharacters.Add(newActiveCharacter);
            }
        }

        Debug.Log($"Updated Player {playerID} character lists: removed {oldActiveCharacter?.Data?.CharacterName}, added {newActiveCharacter?.Data?.CharacterName}");
    }
}