// TraitData.cs
//
// Базовий data‑driven опис рис, який дозволяє сконфігурувати всі риси з constitution.md
// через інспектор без створення окремих C#‑класів під кожну рису.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTrait", menuName = "Game Data/Trait")]
public class TraitData : ScriptableObject
{
    [Header("Trait Info")]
    public string TraitName;

    [TextArea]
    public string Description;

    /// <summary>
    /// Базова вартість у UP (1–5), згідно GDD.
    /// </summary>
    [Range(1, 5)]
    public int Cost = 1;

    /// <summary>
    /// Тип риси: пасивна чи реактивна (активується при кидках 15–17 / 18 тощо).
    /// </summary>
    public TraitKind Kind = TraitKind.Passive;

    /// <summary>
    /// Набір правил "тригер + умова + ефекти".
    /// Однієї TraitData достатньо, щоб описати складну поведінку риси.
    /// </summary>
    public List<TraitRule> Rules = new List<TraitRule>();

    /// <summary>
    /// Викликається, коли риса переходить зі "скритого" у видимий стан (для UI / логів).
    /// Логіка активації самої риси реалізується через TraitSystem і TraitRule.
    /// </summary>
    public virtual void RevealTrait()
    {
        Debug.Log($"Trait {TraitName} is now visible (kind: {Kind}).");
    }
}

// ============================================================================
// ENUMS
// ============================================================================

/// <summary>
/// Загальний тип риси (з точки зору GDD).
/// </summary>
public enum TraitKind
{
    Passive,
    Reactive
}

/// <summary>
/// Точка, у якій риса може спробувати спрацювати.
/// Цього набору тригерів достатньо для рис 1–70 з GDD.
/// </summary>
public enum TraitTriggerType
{
    None,

    // Фаза раунду
    OnRoundStart,
    OnRoundEnd,

    // Атака / захист
    OnAttackAttempt,          // коли персонаж намагається атакувати
    OnAttackRollResolved,     // коли відомий результат 3d6 (hit/miss/crit)
    OnDamageDealt,            // після нанесення шкоди цілі
    OnDamageTaken,            // після отримання шкоди
    OnAttackMissedBySelf,     // власна атака промахнулась
    OnAttackMissedAgainstSelf,// по персонажу промахнулись

    // Рух / позиція
    OnMoveAttempt,
    OnEnterTile,
    OnLeaveTile,

    // Статуси / смерть
    OnStatusApplied,
    OnDeath
}

/// <summary>
/// Типи базових статусів, з якими працюють риси (прокляття, блок руху тощо).
/// Конкретна логіка застосування реалізується у TraitSystem / Character.
/// </summary>
public enum TraitStatusType
{
    None,
    Curse,
    ExtraState,

    BlockMove,
    BlockAttack,

    // Можна розширювати за потреби
}

/// <summary>
/// Категорія результату кидка 3d6 (для умов типу "15–17" / "18").
/// </summary>
public enum HitResultCategory
{
    None,
    Miss,
    NormalHit,
    EnhancedHit,
    CriticalHit
}

/// <summary>
/// Яку сторону модифікуємо / до кого застосовуємо ефект.
/// </summary>
public enum TraitTargetSide
{
    Self,
    Opponent,
    Both
}

/// <summary>
/// Тривалість ефекту.
/// </summary>
public enum TraitEffectDuration
{
    Instant,      // діє одразу й завершився (наприклад, одноразове лікування)
    ThisRoll,     // впливає лише на поточний кидок
    ThisAttack,   // на поточну атаку
    ThisRound,    // до кінця раунду
    Permanent     // поки риса активна
}

/// <summary>
/// Типи умов, які можна задати для правила (комбінуються простими параметрами).
/// </summary>
public enum TraitConditionType
{
    None,

    // 3d6 / результат
    HitCategoryIs,
    RollTotalBetween,

    // Статуси
    SelfHasStatus,
    TargetHasStatus,

    // Стани здоров'я
    SelfHealthStateAtMost,
    SelfHealthStateAtLeast,

    // Разова подія за раунд (наприклад, "перша атака по мені")
    FirstTimeThisRound,

    // Додатковий кидок d6 / 2d6 для стокових рис (регенерація, контратака, віддзеркалення)
    ExtraD6Between
}

/// <summary>
/// Тип ефекту, який може виконувати риса.
/// </summary>
public enum TraitEffectType
{
    None,

    // Модифікатори переваги / порогів
    ModifyAdvantage,          // ± до Advantage/Obstacle
    ModifyCritRange,          // зсуває діапазон крита (наприклад, 17–18 -> 16–18)

    // Вплив на Impact / стан
    Heal,
    DealImpact,
    ReflectImpact,

    // Кидки кубів
    RerollAttackDice,
    RerollDefenseDice,

    // Авто‑дії
    AutoCounterAttack,        // контратака по схемі

    // Блок дій
    BlockMove,
    BlockAttack,

    // Статуси
    ApplyStatus,
    RemoveStatus,

    // Бусти до наступної атаки / по цілі
    MarkNextAttackModifier,

    // Робота з тайлами
    IgnoreTileBonuses,
    IgnoreTileDebuffs,

    // Спеціальні ефекти
    SwapPositions,
    CopyRandomPassiveTrait
}

// ============================================================================
// DATA‑CLASSES
// ============================================================================

/// <summary>
/// Окреме правило риси: "коли X і якщо Y, виконай набір ефектів Z".
/// </summary>
[System.Serializable]
public class TraitRule
{
    public TraitTriggerType Trigger = TraitTriggerType.None;

    [Tooltip("Умова спрацювання (опційно).")]
    public TraitConditionType Condition = TraitConditionType.None;

    [Tooltip("Мінімальне значення (для RollTotalBetween / ExtraD6Between).")]
    public int MinValue = 0;

    [Tooltip("Максимальне значення (для RollTotalBetween / ExtraD6Between).")]
    public int MaxValue = 0;

    [Tooltip("Використовується для HitCategoryIs / HealthState умов.")]
    public HitResultCategory HitCategory = HitResultCategory.None;

    public HealthState MinHealthState = HealthState.Dead;
    public HealthState MaxHealthState = HealthState.Extra;

    [Tooltip("Якщо умова пов'язана зі статусом (SelfHasStatus/TargetHasStatus).")]
    public TraitStatusType Status = TraitStatusType.None;

    [Tooltip("Набір ефектів, які виконуються, коли тригер і умова виконані.")]
    public List<TraitEffect> Effects = new List<TraitEffect>();
}

/// <summary>
/// Опис одного ефекту риси. Частину полів можна ігнорувати залежно від EffectType.
/// </summary>
[System.Serializable]
public class TraitEffect
{
    public TraitEffectType EffectType = TraitEffectType.None;

    [Tooltip("Кого стосується ефект (Self / Opponent / Both).")]
    public TraitTargetSide TargetSide = TraitTargetSide.Self;

    [Tooltip("Тривалість дії ефекту.")]
    public TraitEffectDuration Duration = TraitEffectDuration.Instant;

    // Загальні числові параметри (amount, к‑сть impact, зміна переваги, зсув крита)
    public int IntValue1 = 0;
    public int IntValue2 = 0;

    // Для статусів
    public TraitStatusType Status = TraitStatusType.None;

    // Додатковий прапорець, який можна використати для тонкого налаштування
    public bool Flag;
}