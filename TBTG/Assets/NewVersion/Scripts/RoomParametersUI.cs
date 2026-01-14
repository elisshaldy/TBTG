using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomParametersUI : MonoBehaviour
{
    [SerializeField] private GameObject _bgPlayersMultiplayer; // allow only in multiplayer
    [Space(20)]
    // always show
    [SerializeField] private GameObject _turnTimeUI;
    [SerializeField] private GameObject _fieldSizeUI;
    [SerializeField] private GameObject _partyCountUI;
    [SerializeField] private GameObject _bossCountUI;
    [SerializeField] private GameObject _bossDificultyUI;
    // show if PlayerVSBot state
    [SerializeField] private GameObject _botDificultyUI;
    // show if hotseat
    [SerializeField] private GameObject _hotSeatPlayerName1UI;
    [SerializeField] private GameObject _hotSeatPlayerName2UI;

    [Header("Value Inputs")]
    [SerializeField] private TMP_InputField _turnTimeDropdown;
    [SerializeField] private TMP_Dropdown _fieldSizeDropdown;
    [SerializeField] private TMP_Dropdown _partyCountDropdown;
    [SerializeField] private TMP_Dropdown _bossCountDropdown;
    [SerializeField] private TMP_Dropdown _bossDifficultyDropdown;
    [SerializeField] private TMP_Dropdown _botDifficultyDropdown;
    [SerializeField] private TMP_InputField _namePlayer1;
    [SerializeField] private TMP_InputField _namePlayer2;

    private const int MinTurnTime = 15;
    private const int MaxTurnTime = 180;

    private void Start()
    {
        _turnTimeDropdown.contentType = TMP_InputField.ContentType.IntegerNumber;
        _turnTimeDropdown.characterLimit = 3;
        _turnTimeDropdown.text = "50";

        _turnTimeDropdown.onEndEdit.AddListener(ValidateTurnTime);
        _partyCountDropdown.value = 1;
        _partyCountDropdown.RefreshShownValue();
    }
    
    public void SetInteractable(bool isHost, SceneState state)
    {
        bool interactable = (state == SceneState.Multiplayer) ? isHost : true;

        _turnTimeDropdown.interactable = interactable;

        _fieldSizeDropdown.interactable = interactable;
        _partyCountDropdown.interactable = interactable;
        _bossCountDropdown.interactable = interactable;
        _bossDifficultyDropdown.interactable = interactable;
        _botDifficultyDropdown.interactable = interactable;

        _namePlayer1.interactable = interactable;
        _namePlayer2.interactable = interactable;
    }

    private void ValidateTurnTime(string value)
    {
        if (!int.TryParse(value, out int result))
        {
            _turnTimeDropdown.text = MinTurnTime.ToString();
            return;
        }

        result = Mathf.Clamp(result, MinTurnTime, MaxTurnTime);
        _turnTimeDropdown.text = result.ToString();
    }
    
    public void ShowMultiplayerUI()
    {
        _bgPlayersMultiplayer.SetActive(true);
        _turnTimeUI.SetActive(true);
        _fieldSizeUI.SetActive(true);
        _partyCountUI.SetActive(true);
        _bossCountUI.SetActive(true);
        _bossDificultyUI.SetActive(true);
        
        _botDificultyUI.SetActive(false);
        
        _hotSeatPlayerName1UI.SetActive(false);
        _hotSeatPlayerName2UI.SetActive(false);
    }
    
    public void ShowPlayerVSBotUI()
    {
        _bgPlayersMultiplayer.SetActive(false);
        _turnTimeUI.SetActive(true);
        _fieldSizeUI.SetActive(true);
        _partyCountUI.SetActive(true);
        _bossCountUI.SetActive(true);
        _bossDificultyUI.SetActive(true);
        
        _botDificultyUI.SetActive(true);
        
        _hotSeatPlayerName1UI.SetActive(false);
        _hotSeatPlayerName2UI.SetActive(false);
    }
    
    public void ShowHotseatUI()
    {
        _bgPlayersMultiplayer.SetActive(false);
        _turnTimeUI.SetActive(true);
        _fieldSizeUI.SetActive(true);
        _partyCountUI.SetActive(true);
        _bossCountUI.SetActive(true);
        _bossDificultyUI.SetActive(true);
        
        _botDificultyUI.SetActive(false);
        
        _hotSeatPlayerName1UI.SetActive(true);
        _hotSeatPlayerName2UI.SetActive(true);
    }
}