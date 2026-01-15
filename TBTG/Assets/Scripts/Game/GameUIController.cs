using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private GameObject _containerCards;
    [SerializeField] private GameObject _containerMods;
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _hotseatPlayerNameTxt;
    [SerializeField] private TextMeshProUGUI _modPointsTxt;
    [SerializeField] private Button _applyBtn;
    [Header("State")]
    [SerializeField] private GameSceneState _gameSceneState;
    [SerializeField] private CardDeckController _cardDeckController;
    [SerializeField] private PlayerModel _playerModel;

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
        _applyBtn.gameObject.SetActive(false);
        
        _applyBtn.onClick.AddListener(NextStep);
        _playerModel.OnModPointsChanged += UpdateModPointsUI;
        UpdateModPointsUI(_playerModel.ModPoints);
        
        _gameSceneState.StartFlow(this);
    }
    
    private void UpdateModPointsUI(int value)
    {
        _modPointsTxt.text = $"Mod points: {value}";
    }
    
    private void OnDeckStateChanged(bool isDeckFull)
    {
        _applyBtn.gameObject.SetActive(isDeckFull);
    }
    
    private void NextStep()
    {
        _gameSceneState.Next(this);
    }

    public void OpenCards()
    {
        _containerCards.SetActive(true);
        _containerMods.SetActive(false);
    }

    public void OpenMods()
    {
        _containerCards.SetActive(false);
        _containerMods.SetActive(true);
        _applyBtn.gameObject.SetActive(false);
        _modPointsTxt.gameObject.SetActive(true);
    }
    
    public void OpenMap()
    {
        Debug.Log("Map opened");
        _containerMods.SetActive(false);
        _applyBtn.gameObject.SetActive(false);
    }

    public void OpenHotseatWindow()
    {
        // open window for player 2, after he done OpenMap();
        _hotseatPlayerNameTxt.gameObject.SetActive(true);
    }
    
    public void ShowHotseatPlayer(string playerName)
    {
        _hotseatPlayerNameTxt.text = "Current player: " + playerName;
        _hotseatPlayerNameTxt.gameObject.SetActive(true);
    }
}