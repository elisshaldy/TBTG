using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private GameObject _containerCards;
    [SerializeField] private GameObject _containerMods;
    [Space(20)]
    [SerializeField] private CardDeckController _cardDeckController;
    [SerializeField] private TextMeshProUGUI _hotseatPlayerNameTxt;
    [SerializeField] private Button _applyBtn;

    private void OnEnable()
    {
        _cardDeckController.DeckStateChanged += OnDeckStateChanged;
    }

    private void OnDisable()
    {
        _cardDeckController.DeckStateChanged -= OnDeckStateChanged;
    }

    private void Start()
    {
        _applyBtn.gameObject.SetActive(false);
        _hotseatPlayerNameTxt.gameObject.SetActive(false);
        
        _applyBtn.onClick.AddListener(OpenMods);
    }

    private void OnDeckStateChanged(bool isDeckFull)
    {
        _applyBtn.gameObject.SetActive(isDeckFull);
    }

    private void OpenMods()
    {
        _containerMods.SetActive(true);
        _containerCards.SetActive(false);
    }
}