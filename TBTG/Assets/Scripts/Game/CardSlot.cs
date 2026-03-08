using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class CardSlot : MonoBehaviour, IDropHandler
{
    public event Action CardAdded;
    public event Action CardRemoved;
    
    public RectTransform SlotPoint;

    public CardDragHandler CurrentCard;
    
    [SerializeField] private Color _zeroHealth;
    [SerializeField] private Color _oneHealth;
    [SerializeField] private Color _extraHealth;
    
    [SerializeField] private Image[] _healthBars = new Image[6];

    private void Awake()
    {
        if (SlotPoint == null)
            SlotPoint = transform as RectTransform;
            
        UpdateHealthUI();
    }

    private void OnEnable()
    {
        CharacterHealthSystem.OnAnyHealthChanged += UpdateHealthUI;
    }

    private void OnDisable()
    {
        CharacterHealthSystem.OnAnyHealthChanged -= UpdateHealthUI;
    }

    public bool IsOccupied => CurrentCard != null;

    public void OnDrop(PointerEventData eventData)
    {
        CardDragHandler incomingCard = eventData.pointerDrag.GetComponent<CardDragHandler>();
        if (incomingCard == null) return;
        
        if (incomingCard.IsLockedInSlot && incomingCard.LastSlot != this) return;

        if (IsOccupied)
        {
            CardDragHandler existingCard = CurrentCard;
            CardSlot previousSlot = incomingCard.LastSlot;

            if (previousSlot != null)
            {
                // SWAP
                previousSlot.SetCardManually(existingCard);
            }
            else
            {
                existingCard.ReturnHome();
                existingCard.CurrentSlot = null;
            }
        }

        CurrentCard = incomingCard;
        incomingCard.CurrentSlot = this;

        incomingCard.SnapToSlot(SlotPoint);
        UpdateHealthUI();
        CardAdded?.Invoke();
    }

    public void SetCardManually(CardDragHandler card)
    {
        CurrentCard = card;
        card.CurrentSlot = this;
        card.SnapToSlot(SlotPoint);
        UpdateHealthUI();
        CardAdded?.Invoke();
    }

    public void ClearSlot()
    {
        CurrentCard = null;
        UpdateHealthUI();
        CardRemoved?.Invoke();
    }

    [ContextMenu("Force Update Health UI")]
    public void UpdateHealthUI()
    {
        if (CurrentCard == null)
        {
            ClearHealthBars();
            return;
        }

        // 1. Try to find the REAL health from the character on the board
        CharacterHealthSystem healthSystem = null;
        if (CharacterPlacementManager.Instance != null)
        {
            var charObj = CharacterPlacementManager.Instance.GetCharacterObject(CurrentCard.OwnerID, CurrentCard.PairID);
            if (charObj != null)
            {
                healthSystem = charObj.GetComponentInChildren<CharacterHealthSystem>(true);
            }
        }

        // 2. Fallback to the card's local health system if not on map
        if (healthSystem == null)
        {
            healthSystem = CurrentCard.GetComponentInChildren<CharacterHealthSystem>(true);
        }

        int activeBars = 0; 
        Color activeColor = _oneHealth;

        if (healthSystem != null)
        {
            switch (healthSystem.HealthState)
            {
                case CharacterHealthSystem.CharHealth.Dead:
                    activeBars = 0;
                    break;
                case CharacterHealthSystem.CharHealth.Coma:
                    activeBars = 1;
                    break;
                case CharacterHealthSystem.CharHealth.Critical:
                    activeBars = 2;
                    break;
                case CharacterHealthSystem.CharHealth.Serious:
                    activeBars = 3;
                    break;
                case CharacterHealthSystem.CharHealth.Minor:
                    activeBars = 4;
                    break;
                case CharacterHealthSystem.CharHealth.Normal:
                case CharacterHealthSystem.CharHealth.NonInitialized:
                    activeBars = 5;
                    break;
                case CharacterHealthSystem.CharHealth.Extra:
                    activeBars = 6;
                    activeColor = _extraHealth;
                    break;
            }
        }

        if (_healthBars == null || _healthBars.Length == 0) return;

        // Visual fix: ensure colors are visible even if unset in Inspector
        if (activeColor.a < 0.1f) activeColor = Color.green;
        Color emptyColor = (_zeroHealth.a < 0.1f) ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : _zeroHealth;

        for (int i = 0; i < _healthBars.Length; i++)
        {
            if (_healthBars[i] == null) continue;

            if (i < activeBars)
                _healthBars[i].color = activeColor;
            else
                _healthBars[i].color = emptyColor;
        }
    }

    private void ClearHealthBars()
    {
        if (_healthBars == null) return;

        for (int i = 0; i < _healthBars.Length; i++)
        {
            if (_healthBars[i] != null)
                _healthBars[i].color = _zeroHealth;
        }
    }
}