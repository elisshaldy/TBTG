using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private GameObject _containerCards;
    [SerializeField] private GameObject _containerMods;
    [SerializeField] private GameObject _map;
    [SerializeField] private GameObject _mapUIGameLoop;
    [Header("UI")]
    [SerializeField] private LocalizationLabel _hotseatPlayerNameTxt;
    [SerializeField] private LocalizationLabel _modPointsTxt;
    [SerializeField] private Button _applyBtn;
    [Header("State")]
    [SerializeField] private GameSceneState _gameSceneState;
    [SerializeField] private CardDeckController _cardDeckController;
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private GameDataInitializer _dataInitializer;
    private Button _debugAutoFillBtn;
    private Button _debugAutoFillModsBtn;
    private Button _debugFullAutoBtn;

    public void EnableDeckListening()
    {
        _cardDeckController.DeckStateChanged += OnDeckStateChanged;
    }

    private void OnDisable()
    {
        DisableDeckListening();
        _playerModel.OnModPointsChanged -= UpdateModPointsUI;
    }
    
    public void DisableDeckListening()
    {
        _cardDeckController.DeckStateChanged -= OnDeckStateChanged;
        _cardDeckController.UnsubscribeSlots();
    }

    private void Start()
    {
        if (_dataInitializer == null) _dataInitializer = FindObjectOfType<GameDataInitializer>();
        
        _applyBtn.gameObject.SetActive(false);
        
        _applyBtn.onClick.AddListener(NextStep);
        _playerModel.OnModPointsChanged += UpdateModPointsUI;
        UpdateModPointsUI(_playerModel.ModPoints);
        
        _gameSceneState.StartFlow(this);
        if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.IsDebug) CreateDebugButtons();
        
        _map.SetActive(false);
        _mapUIGameLoop.SetActive(false);
    }

    private void CreateDebugButtons()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Vector2 startPos = new Vector2(-20, -20);
        float verticalOffset = 60f;

        _debugAutoFillBtn = CreateButton(canvas, "DEBUG: AUTO CARDS", startPos, Color.yellow, () => _cardDeckController.AutoFillSlots());
        _debugAutoFillModsBtn = CreateButton(canvas, "DEBUG: AUTO MODS", startPos + new Vector2(0, -verticalOffset), Color.cyan, () => _cardDeckController.AutoFillMods());
        _debugFullAutoBtn = CreateButton(canvas, "DEBUG: FULL AUTO", startPos + new Vector2(0, -verticalOffset * 2), Color.green, FullAuto);

        UpdateDebugBtnVisibility();
    }

    private Button CreateButton(Canvas canvas, string label, Vector2 pos, Color textColor, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(label + "Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160, 50);
        
        btnObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        var txt = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 14;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color = textColor;
        
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        
        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(action);
        return btn;
    }

    private void UpdateDebugBtnVisibility()
    {
        bool isDebug = GameSettingsManager.Instance != null && GameSettingsManager.Instance.IsDebug;
        if (!isDebug)
        {
            if (_debugAutoFillBtn != null) _debugAutoFillBtn.gameObject.SetActive(false);
            if (_debugAutoFillModsBtn != null) _debugAutoFillModsBtn.gameObject.SetActive(false);
            if (_debugFullAutoBtn != null) _debugFullAutoBtn.gameObject.SetActive(false);
            return;
        }

        var step = _gameSceneState.CurrentStep;
        if (_debugAutoFillBtn != null) _debugAutoFillBtn.gameObject.SetActive(step == GameSetupStep.Cards);
        if (_debugAutoFillModsBtn != null) _debugAutoFillModsBtn.gameObject.SetActive(step == GameSetupStep.Mods);
        if (_debugFullAutoBtn != null) _debugFullAutoBtn.gameObject.SetActive(step == GameSetupStep.Cards || step == GameSetupStep.Mods);
    }

    private void FullAuto()
    {
        if (_gameSceneState.CurrentStep == GameSetupStep.Cards)
        {
            _cardDeckController.AutoFillSlots();
            NextStep(); // Moves to Mods
        }
        
        // This is not an 'else' because we might want to fill mods right after cards
        if (_gameSceneState.CurrentStep == GameSetupStep.Mods)
        {
            _cardDeckController.AutoFillMods();
            NextStep(); // Moves to Map
        }
    }
    
    private void UpdateModPointsUI(int value)
    {
        _modPointsTxt.SetKey("mod_points_game");
        _modPointsTxt.SetSuffix(": " + value); 
    }
    
    private void OnDeckStateChanged(bool isDeckFull)
    {
        _applyBtn.gameObject.SetActive(isDeckFull);
    }
    
    private void NextStep()
    {
        // Створюємо снепшот перед переходом на наступний крок (якщо ми на картах або модах)
        if (_gameSceneState.CurrentStep == GameSetupStep.Cards || _gameSceneState.CurrentStep == GameSetupStep.Mods)
        {
            var selected = _cardDeckController.GetSelectedCards();
            _gameSceneState._currentSettings.TakeSnapshot(selected);
        }

        _gameSceneState.Next(this);
        UpdateDebugBtnVisibility();
    }

    public void OpenCards()
    {
        _containerCards.SetActive(true);
        _containerMods.SetActive(false);
        _modPointsTxt.gameObject.SetActive(false); // Вимикаємо текст балів при виборі карт
        
        _cardDeckController.ResetController();
        _playerModel.ResetModPoints();
        _dataInitializer.InitializeGame();
        UpdateDebugBtnVisibility();
    }

    public void OpenMods()
    {
        _containerCards.SetActive(false);
        _containerMods.SetActive(true);
        _applyBtn.gameObject.SetActive(true);
        _modPointsTxt.gameObject.SetActive(true); // Вмикаємо тільки на модах
    }
    
    public void OpenMap()
    {
        DisableDeckListening();
        // Debug.Log("Map opened");
        
        bool isHotseatMap = _gameSceneState != null && 
                           _gameSceneState._currentSettings is HotseatSettings;

        // В Hotseat нам ТРЕБА бачити контейнери (або хоча б карти в слотах), 
        // тому не вимикаємо все підряд, якщо це тільки початок розстановки
        if (!isHotseatMap)
        {
            _containerMods.SetActive(false);
            _containerCards.SetActive(false);
        }
        else
        {
            // В Hotseat на карті нам потрібні тільки персонажі для розстановки,
            // а модифікатори вже мають бути приховані, щоб не заважати фоном.
            _containerMods.SetActive(false);
            _containerCards.SetActive(false); // Ми їх увімкнемо через слоти деки, якщо треба
        }

        _modPointsTxt.gameObject.SetActive(false);
        _applyBtn.gameObject.SetActive(false);
        _map.SetActive(true);
        _mapUIGameLoop.SetActive(true);
        
        if (_dataInitializer != null) _dataInitializer.CleanUpContainers();
    }

    public void OpenHotseatWindow()
    {
        // open window for player 2
        _hotseatPlayerNameTxt.gameObject.SetActive(true);
        // Debug.Log("REINITIALIZE");
        _containerCards.SetActive(false);
        _containerMods.SetActive(false);
        // here Hotseat Important !!!!!
        // here Next Step
        // reinitialize for 2 player
    }
    
    public void ShowHotseatPlayer(string playerName)
    {
        _hotseatPlayerNameTxt.SetKey("hotseat_current_player");
        _hotseatPlayerNameTxt.SetSuffix(": " + playerName);
        _hotseatPlayerNameTxt.gameObject.SetActive(true);
    }
}