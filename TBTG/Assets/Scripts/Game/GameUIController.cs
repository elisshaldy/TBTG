using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private GameObject _containerCards;
    [SerializeField] private GameObject _containerMods;
    [SerializeField] private GameObject _map;
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
        if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.IsDebug) CreateDebugButton();
    }

    private void CreateDebugButton()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject btnObj = new GameObject("DebugAutoFillBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(160, 50);
        
        btnObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        var txt = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        txt.text = "DEBUG: FILL CARDS";
        txt.fontSize = 14;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color = Color.yellow;
        
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        
        _debugAutoFillBtn = btnObj.GetComponent<Button>();
        _debugAutoFillBtn.onClick.AddListener(() => _cardDeckController.AutoFillSlots());
        
        UpdateDebugBtnVisibility();
    }

    private void UpdateDebugBtnVisibility()
    {
        bool isDebug = GameSettingsManager.Instance != null && GameSettingsManager.Instance.IsDebug;
        if (isDebug && _debugAutoFillBtn != null)
        {
            _debugAutoFillBtn.gameObject.SetActive(_gameSceneState.CurrentStep == GameSetupStep.Cards);
        }
        else if (_debugAutoFillBtn != null)
        {
            _debugAutoFillBtn.gameObject.SetActive(false);
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
        Debug.Log("Map opened");
        _containerMods.SetActive(false);
        _modPointsTxt.gameObject.SetActive(false);
        _applyBtn.gameObject.SetActive(false);
        _map.SetActive(true);
    }

    public void OpenHotseatWindow()
    {
        // open window for player 2
        _hotseatPlayerNameTxt.gameObject.SetActive(true);
        Debug.Log("REINITIALIZE");
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