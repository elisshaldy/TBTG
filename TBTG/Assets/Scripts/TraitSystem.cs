// TraitSystem.cs
//
// Централізована система обробки рис (TraitData) за data‑driven моделлю.
// На даному етапі реалізуємо базову підтримку тригерів, які потрібні для бою:
// - модифікатори переваги (Advantage) до атаки;
// - прості реактивні ефекти після влучання (Heal / DealImpact / ReflectImpact).
//
// Інші типи ефектів/умов уже описані в TraitData і можуть бути поступово
// реалізовані в цьому класі без змін у TraitData.

using System.Collections.Generic;
using UnityEngine;

public static class TraitSystem
{
    /// <summary>
    /// Контекст для оцінки правила риси.
    /// </summary>
    public struct TraitContext
    {
        public Character Self;
        public Character Other;

        public int RollTotal;
        public int EffectiveRoll;
        public HitResultCategory HitCategory;

        public bool IsAttacker; // true, якщо Self є атакуючим у цій взаємодії
    }

    // ----------------------------------------------------------------------
    // ПАСИВНІ МОДИФІКАТОРИ ДО ПЕРЕВАГИ (ADVANTAGE)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Обчислити додатковий модифікатор переваги від пасивних рис атакера/цілі.
    /// Повертає сумарний delta, який додається до вже порахованої переваги карти/тайлів.
    /// </summary>
    public static int GetAdvantageModifier(Character attacker, Character target)
    {
        int delta = 0;

        if (attacker != null)
        {
            TraitContext ctx = new TraitContext
            {
                Self = attacker,
                Other = target,
                IsAttacker = true
            };

            delta += EvaluateTraitsForAdvantage(attacker, ctx);
        }

        if (target != null)
        {
            TraitContext ctx = new TraitContext
            {
                Self = target,
                Other = attacker,
                IsAttacker = false
            };

            delta += EvaluateTraitsForAdvantage(target, ctx);
        }

        return delta;
    }

    private static int EvaluateTraitsForAdvantage(Character owner, TraitContext ctx)
    {
        int delta = 0;
        var traits = owner?.GetActiveTraits();
        if (traits == null) return 0;

        foreach (TraitData trait in traits)
        {
            if (trait == null) continue;

            foreach (TraitRule rule in trait.Rules)
            {
                if (rule == null) continue;
                if (rule.Trigger != TraitTriggerType.OnAttackAttempt) continue;

                if (!CheckCondition(rule, ctx)) continue;

                foreach (TraitEffect effect in rule.Effects)
                {
                    if (effect == null) continue;
                    if (effect.EffectType != TraitEffectType.ModifyAdvantage) continue;

                    // IntValue1 використовується як величина зміни переваги (±).
                    delta += effect.IntValue1;
                }
            }
        }

        return delta;
    }

    // ----------------------------------------------------------------------
    // РЕАКТИВНІ РИСИ ПІСЛЯ ВИЗНАЧЕННЯ РЕЗУЛЬТАТУ АТАКИ
    // ----------------------------------------------------------------------

    /// <summary>
    /// Обробка реактивних рис після того, як відомий результат атаки.
    /// impactsToSelf/impactsToOther дозволяють рисам додати/зняти impact зі
    /// сторін (Self = власник риси, Other = інша сторона).
    /// </summary>
    public static void OnAttackRollResolved(
        Character attacker,
        Character target,
        int roll,
        int effectiveRoll,
        HitResultCategory hitCategory,
        int baseImpacts,
        out int extraImpactsToAttacker,
        out int extraImpactsToTarget)
    {
        extraImpactsToAttacker = 0;
        extraImpactsToTarget = 0;

        if (attacker == null || target == null) return;

        // 1. Риси атакуючого
        ApplyReactiveTraitsForSide(
            owner: attacker,
            other: target,
            isAttacker: true,
            roll: roll,
            effectiveRoll: effectiveRoll,
            hitCategory: hitCategory,
            baseImpacts: baseImpacts,
            ref extraImpactsToAttacker,
            ref extraImpactsToTarget);

        // 2. Риси цілі
        ApplyReactiveTraitsForSide(
            owner: target,
            other: attacker,
            isAttacker: false,
            roll: roll,
            effectiveRoll: effectiveRoll,
            hitCategory: hitCategory,
            baseImpacts: baseImpacts,
            ref extraImpactsToTarget,
            ref extraImpactsToAttacker);
    }

    private static void ApplyReactiveTraitsForSide(
        Character owner,
        Character other,
        bool isAttacker,
        int roll,
        int effectiveRoll,
        HitResultCategory hitCategory,
        int baseImpacts,
        ref int extraImpactsToOwner,
        ref int extraImpactsToOther)
    {
        var traits = owner?.GetActiveTraits();
        if (traits == null) return;

        TraitContext ctx = new TraitContext
        {
            Self = owner,
            Other = other,
            RollTotal = roll,
            EffectiveRoll = effectiveRoll,
            HitCategory = hitCategory,
            IsAttacker = isAttacker
        };

        foreach (TraitData trait in traits)
        {
            if (trait == null) continue;
            if (trait.Kind != TraitKind.Reactive) continue;

            foreach (TraitRule rule in trait.Rules)
            {
                if (rule == null) continue;
                if (rule.Trigger != TraitTriggerType.OnAttackRollResolved) continue;

                if (!CheckCondition(rule, ctx)) continue;

                foreach (TraitEffect effect in rule.Effects)
                {
                    if (effect == null) continue;

                    ApplyReactiveEffect(
                        effect,
                        ctx,
                        baseImpacts,
                        ref extraImpactsToOwner,
                        ref extraImpactsToOther);
                }
            }
        }
    }

    private static void ApplyReactiveEffect(
        TraitEffect effect,
        TraitContext ctx,
        int baseImpacts,
        ref int extraImpactsToOwner,
        ref int extraImpactsToOther)
    {
        switch (effect.EffectType)
        {
            case TraitEffectType.Heal:
                // IntValue1 = к‑сть impact, яку знімаємо з власника.
                // Представляємо лікування як негативний impact.
                if (effect.TargetSide == TraitTargetSide.Self || effect.TargetSide == TraitTargetSide.Both)
                {
                    extraImpactsToOwner -= effect.IntValue1;
                }
                if (effect.TargetSide == TraitTargetSide.Opponent || effect.TargetSide == TraitTargetSide.Both)
                {
                    extraImpactsToOther -= effect.IntValue1;
                }
                break;

            case TraitEffectType.DealImpact:
                // Додаткові impact-и по цілі / собі
                if (effect.TargetSide == TraitTargetSide.Self || effect.TargetSide == TraitTargetSide.Both)
                {
                    extraImpactsToOwner += effect.IntValue1;
                }
                if (effect.TargetSide == TraitTargetSide.Opponent || effect.TargetSide == TraitTargetSide.Both)
                {
                    extraImpactsToOther += effect.IntValue1;
                }
                break;

            case TraitEffectType.ReflectImpact:
                // Віддзеркалення baseImpacts назад іншій стороні (1:1).
                if (baseImpacts <= 0) return;

                // Вважаємо, що віддзеркалення завжди б'є по Opponent
                extraImpactsToOther += baseImpacts;
                break;

            case TraitEffectType.BlockMove:
                // Блокування руху через статус
                if (effect.TargetSide == TraitTargetSide.Self || effect.TargetSide == TraitTargetSide.Both)
                {
                    ApplyStatusToCharacter(ctx.Self, TraitStatusType.BlockMove, effect.Duration);
                }
                if (effect.TargetSide == TraitTargetSide.Opponent || effect.TargetSide == TraitTargetSide.Both)
                {
                    ApplyStatusToCharacter(ctx.Other, TraitStatusType.BlockMove, effect.Duration);
                }
                break;

            case TraitEffectType.BlockAttack:
                // Блокування атаки через статус
                if (effect.TargetSide == TraitTargetSide.Self || effect.TargetSide == TraitTargetSide.Both)
                {
                    ApplyStatusToCharacter(ctx.Self, TraitStatusType.BlockAttack, effect.Duration);
                }
                if (effect.TargetSide == TraitTargetSide.Opponent || effect.TargetSide == TraitTargetSide.Both)
                {
                    ApplyStatusToCharacter(ctx.Other, TraitStatusType.BlockAttack, effect.Duration);
                }
                break;

            case TraitEffectType.ApplyStatus:
                // Застосування статусу (IntValue1 = TraitStatusType як enum value)
                TraitStatusType statusToApply = (TraitStatusType)effect.IntValue1;
                if (statusToApply != TraitStatusType.None)
                {
                    if (effect.TargetSide == TraitTargetSide.Self || effect.TargetSide == TraitTargetSide.Both)
                    {
                        ApplyStatusToCharacter(ctx.Self, statusToApply, effect.Duration);
                    }
                    if (effect.TargetSide == TraitTargetSide.Opponent || effect.TargetSide == TraitTargetSide.Both)
                    {
                        ApplyStatusToCharacter(ctx.Other, statusToApply, effect.Duration);
                    }
                }
                break;

            case TraitEffectType.RemoveStatus:
                // Зняття статусу
                TraitStatusType statusToRemove = (TraitStatusType)effect.IntValue1;
                if (statusToRemove != TraitStatusType.None)
                {
                    if (effect.TargetSide == TraitTargetSide.Self || effect.TargetSide == TraitTargetSide.Both)
                    {
                        RemoveStatusFromCharacter(ctx.Self, statusToRemove);
                    }
                    if (effect.TargetSide == TraitTargetSide.Opponent || effect.TargetSide == TraitTargetSide.Both)
                    {
                        RemoveStatusFromCharacter(ctx.Other, statusToRemove);
                    }
                }
                break;

            case TraitEffectType.SwapPositions:
                // Обмін позиціями між персонажами
                if (ctx.Self != null && ctx.Other != null)
                {
                    SwapCharacterPositions(ctx.Self, ctx.Other);
                }
                break;

            // Інші типи ефектів будуть реалізовані поступово.
            default:
                Debug.LogWarning($"TraitEffectType {effect.EffectType} is not yet implemented.");
                break;
        }
    }

    // ----------------------------------------------------------------------
    // ДОПОМІЖНІ МЕТОДИ ДЛЯ ЕФЕКТІВ
    // ----------------------------------------------------------------------

    /// <summary>
    /// Застосувати статус до персонажа на задану тривалість.
    /// </summary>
    private static void ApplyStatusToCharacter(Character character, TraitStatusType status, TraitEffectDuration duration)
    {
        if (character == null || status == TraitStatusType.None) return;

        character.AddStatus(status);
        Debug.Log($"[TraitSystem] Applied status {status} to {character.Data.CharacterName} (duration: {duration})");

        // TODO: Реалізувати систему тривалості статусів (якщо duration != Permanent)
        // Наразі всі статуси застосовуються постійно до ручного зняття
    }

    /// <summary>
    /// Зняти статус з персонажа.
    /// </summary>
    private static void RemoveStatusFromCharacter(Character character, TraitStatusType status)
    {
        if (character == null || status == TraitStatusType.None) return;

        character.RemoveStatus(status);
        Debug.Log($"[TraitSystem] Removed status {status} from {character.Data.CharacterName}");
    }

    /// <summary>
    /// Обміняти позиції двох персонажів на полі.
    /// </summary>
    private static void SwapCharacterPositions(Character char1, Character char2)
    {
        if (char1 == null || char2 == null) return;

        Vector2Int pos1 = char1.GridPosition;
        Vector2Int pos2 = char2.GridPosition;

        Tile tile1 = GridManager.Instance.GetTile(pos1);
        Tile tile2 = GridManager.Instance.GetTile(pos2);

        if (tile1 == null || tile2 == null)
        {
            Debug.LogError("[TraitSystem] Cannot swap positions: tiles not found.");
            return;
        }

        // Оновлюємо позиції персонажів
        char1.GridPosition = pos2;
        char2.GridPosition = pos1;

        // Оновлюємо візуальні позиції
        char1.transform.position = tile2.transform.position;
        char2.transform.position = tile1.transform.position;

        // Оновлюємо зайнятість тайлів
        tile1.RemoveOccupant();
        tile2.RemoveOccupant();
        tile1.SetOccupant(char2);
        tile2.SetOccupant(char1);

        Debug.Log($"[TraitSystem] Swapped positions: {char1.Data.CharacterName} <-> {char2.Data.CharacterName}");
    }

    // ----------------------------------------------------------------------
    // ПЕРЕВІРКИ БЛОКУВАННЯ ДІЙ
    // ----------------------------------------------------------------------

    /// <summary>
    /// Перевіряє, чи заблокований рух для персонажа через риси (статус BlockMove).
    /// </summary>
    public static bool IsMoveBlocked(Character character)
    {
        if (character == null) return false;
        return character.HasStatus(TraitStatusType.BlockMove);
    }

    /// <summary>
    /// Перевіряє, чи заблокована атака для персонажа через риси (статус BlockAttack).
    /// </summary>
    public static bool IsAttackBlocked(Character character)
    {
        if (character == null) return false;
        return character.HasStatus(TraitStatusType.BlockAttack);
    }

    // ----------------------------------------------------------------------
    // УМОВИ
    // ----------------------------------------------------------------------

    private static bool CheckCondition(TraitRule rule, TraitContext ctx)
    {
        switch (rule.Condition)
        {
            case TraitConditionType.None:
                return true;

            case TraitConditionType.HitCategoryIs:
                return ctx.HitCategory == rule.HitCategory;

            case TraitConditionType.RollTotalBetween:
                return ctx.EffectiveRoll >= rule.MinValue && ctx.EffectiveRoll <= rule.MaxValue;

            case TraitConditionType.SelfHealthStateAtMost:
                return ctx.Self != null && ctx.Self.CurrentState <= rule.MaxHealthState;

            case TraitConditionType.SelfHealthStateAtLeast:
                return ctx.Self != null && ctx.Self.CurrentState >= rule.MinHealthState;

            case TraitConditionType.SelfHasStatus:
                return ctx.Self != null && ctx.Self.HasStatus(rule.Status);

            case TraitConditionType.TargetHasStatus:
                return ctx.Other != null && ctx.Other.HasStatus(rule.Status);

            default:
                // Інші типи умов ще не реалізовані – вважаємо, що не виконані,
                // щоб випадково не активувати складні риси.
                return false;
        }
    }
}