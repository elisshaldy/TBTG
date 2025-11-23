// TraitSlot.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Слот для розміщення рис на картках персонажів.
/// Аналог DropSlot, але для TraitData.
/// </summary>
public class TraitSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Settings")]
    public CharacterData AssociatedCharacter; // Персонаж, до якого належить цей слот

    [Header("Visual Feedback")]
    public Image BackgroundImage;
    public Color NormalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color HighlightColor = new Color(0.5f, 0.8f, 0.5f, 0.7f);
    public Color OccupiedColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
    public Color InvalidColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);

    [Header("Additional Visual Feedback")]
    public GameObject DropIndicator;

    private TraitCardHandler _currentTrait;
    public TraitCardHandler CurrentTrait
    {
        get => _currentTrait;
        private set
        {
            _currentTrait = value;
            UpdateVisuals();
        }
    }

    void Start()
    {
        UpdateVisuals();
    }

    public void OnDrop(PointerEventData eventData)
    {
        TraitCardHandler draggedTrait = eventData.pointerDrag?.GetComponent<TraitCardHandler>();

        if (draggedTrait != null)
        {
            AcceptTrait(draggedTrait);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging)
        {
            TraitCardHandler draggedTrait = eventData.pointerDrag?.GetComponent<TraitCardHandler>();
            if (draggedTrait != null && CanAcceptTrait(draggedTrait))
            {
                ShowCanDropFeedback();
            }
            else
            {
                ShowCannotDropFeedback();
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisuals();
    }

    public void AcceptTrait(TraitCardHandler newTrait)
    {
        Debug.Log($"AcceptTrait called for {newTrait.TraitData.TraitName} in slot {name}");

        if (CurrentTrait == newTrait)
        {
            Debug.Log("Same trait, no replacement needed");
            return;
        }

        if (!CanAcceptTrait(newTrait))
        {
            Debug.Log($"Cannot accept trait {newTrait.TraitData.TraitName} in slot {name}");
            return;
        }

        TraitCardHandler oldTrait = CurrentTrait;

        RemoveTraitFromPreviousSlot(newTrait);

        CurrentTrait = newTrait;
        newTrait.MoveToSlot(transform, this);
        newTrait.OnPlacedInSlot(this);

        if (oldTrait != null)
        {
            Debug.Log($"Replacing old trait {oldTrait.TraitData.TraitName} with new trait {newTrait.TraitData.TraitName}");
            oldTrait.ReturnToPool();
        }

        Debug.Log($"Trait {newTrait.TraitData.TraitName} successfully placed in slot {name}");

        UpdateVisuals();
    }

    /// <summary>
    /// Перевіряє, чи може слот прийняти цю рису.
    /// Тут можна додати перевірки на дублікати, UP баланс тощо.
    /// </summary>
    public bool CanAcceptTrait(TraitCardHandler trait)
    {
        if (trait == null || trait.TraitData == null) return false;
        if (trait == CurrentTrait) return false;
        if (AssociatedCharacter == null) return false;

        // Перевірка на дублікати: чи вже є ця риса у персонажа?
        if (AssociatedCharacter.PurchasedTraits.Contains(trait.TraitData))
        {
            Debug.LogWarning($"Character {AssociatedCharacter.CharacterName} already has trait {trait.TraitData.TraitName}");
            return false;
        }

        // Перевірка UP балансу (буде викликатись з TraitPurchaseManager)
        if (TraitPurchaseManager.Instance != null)
        {
            if (!TraitPurchaseManager.Instance.CanAffordTrait(AssociatedCharacter, trait.TraitData))
            {
                Debug.LogWarning($"Cannot afford trait {trait.TraitData.TraitName} for {AssociatedCharacter.CharacterName}");
                return false;
            }
        }

        return true;
    }

    private void RemoveTraitFromPreviousSlot(TraitCardHandler trait)
    {
        if (trait == null) return;

        TraitSlot[] allSlots = FindObjectsOfType<TraitSlot>();
        foreach (TraitSlot slot in allSlots)
        {
            if (slot != this && slot.CurrentTrait == trait)
            {
                Debug.Log($"Removing trait {trait.TraitData.TraitName} from previous slot: {slot.name}");
                slot.ClearTraitWithoutReturning();
                break;
            }
        }
    }

    public void RemoveTrait()
    {
        if (CurrentTrait != null)
        {
            Debug.Log($"Removing trait {CurrentTrait.TraitData.TraitName} from slot {name}");
            CurrentTrait.OnRemovedFromSlot();
            CurrentTrait.ReturnToPool();
            CurrentTrait = null;
        }
    }

    public void ClearTraitWithoutReturning()
    {
        if (CurrentTrait != null)
        {
            Debug.Log($"Clearing trait {CurrentTrait.TraitData.TraitName} from slot {name} without returning");
            CurrentTrait.OnRemovedFromSlot();
            CurrentTrait = null;
        }
    }

    private void UpdateVisuals()
    {
        if (BackgroundImage != null)
        {
            if (CurrentTrait != null)
            {
                BackgroundImage.color = OccupiedColor;
            }
            else
            {
                BackgroundImage.color = NormalColor;
            }
        }

        if (DropIndicator != null)
        {
            DropIndicator.SetActive(CurrentTrait == null);
        }
    }

    public void ShowCanDropFeedback()
    {
        if (BackgroundImage != null)
            BackgroundImage.color = HighlightColor;
    }

    public void ShowCannotDropFeedback()
    {
        if (BackgroundImage != null)
            BackgroundImage.color = InvalidColor;
    }

    public bool IsOccupied() => CurrentTrait != null;
    public TraitData GetTraitData() => CurrentTrait?.TraitData;
}

