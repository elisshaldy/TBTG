// TraitPurchaseManager.cs (оновлений)

// ... (частина з using та Singleton залишається без змін) ...
// (Якщо ви визначали SelectionMode в PlayerHandData, то тут знадобиться using.)

using UnityEngine;

public class TraitPurchaseManager : MonoBehaviour
{
    public static TraitPurchaseManager Instance { get; private set; }

    // ... (Awake() залишається без змін) ...

    // ----------------------------------------------------------------------
    // ОСНОВНИЙ МЕТОД ФАЗИ (Тепер приймає дані обох гравців)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Запускає фазу купівлі рис, використовуючи обрані руки гравців.
    /// </summary>
    public void StartPurchasePhase(PlayerHandData player1Hand, PlayerHandData player2Hand)
    {
        Debug.Log("Game Phase: Trait Purchase Phase Initiated.");

        // TODO: Тут має бути реалізована логіка:
        // 1. Зберегти посилання на player1Hand та player2Hand.
        // 2. Визначити поточного гравця (P1) та відобразити його UI для купівлі/вибору рис.
        // 3. UI має використовувати PlayerHandData.GetSelectionMode() для відображення Visible/Hidden карт.

        // Після завершення купівлі обома гравцями:
        // GameManager.Instance.StartPlacementPhase();
    }
}