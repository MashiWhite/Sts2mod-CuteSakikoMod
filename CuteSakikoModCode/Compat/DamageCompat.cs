using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Compat;

/// <summary>
/// 伤害指令的跨版本包装。
/// 0.107：Damage 没有 CardPlay 参数（最多 dealer + cardSource），多目标只有 dealer 版本；
/// 0.111+：支持 dealer + cardSource + cardPlay，多目标也支持。
/// 调用点统一走这里，避免每个文件里写 #if。
/// </summary>
internal static class DamageCompat
{
    private static (Creature? Dealer, CardModel? Card, CardPlay? Play) Split(object?[] extras)
    {
        Creature? dealer = null;
        CardModel? card = null;
        CardPlay? play = null;
        foreach (var item in extras)
        {
            switch (item)
            {
                case Creature creature:
                    dealer = creature;
                    break;
                case CardModel model:
                    card = model;
                    break;
                case CardPlay cardPlay:
                    play = cardPlay;
                    break;
            }
        }

        return (dealer, card, play);
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext ctx, Creature target, DamageVar damageVar, params object?[] extras)
    {
        var (dealer, card, play) = Split(extras);
#if CSK_GAME_111_OR_NEWER
        if (play != null || card != null)
            return CreatureCmd.Damage(ctx, target, damageVar, dealer, card, play);
        if (dealer != null)
            return CreatureCmd.Damage(ctx, target, damageVar, dealer);
        throw new InvalidOperationException("Damage 需要 dealer 或 cardSource");
#else
        if (card != null)
            return CreatureCmd.Damage(ctx, target, damageVar, dealer, card);
        if (dealer != null)
            return CreatureCmd.Damage(ctx, target, damageVar, dealer);
        throw new InvalidOperationException("0.107 Damage 需要 dealer 或 cardSource");
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext ctx, IEnumerable<Creature> targets, DamageVar damageVar, params object?[] extras)
    {
        var (dealer, card, play) = Split(extras);
#if CSK_GAME_111_OR_NEWER
        if (play != null || card != null)
            return CreatureCmd.Damage(ctx, targets, damageVar, dealer, card, play);
        return CreatureCmd.Damage(ctx, targets, damageVar, dealer);
#else
        if (dealer != null)
            return CreatureCmd.Damage(ctx, targets, damageVar, dealer);
        if (card != null)
            return DamageEach(ctx, targets, damageVar, card);
        throw new InvalidOperationException("0.107 多目标 Damage 需要 dealer");
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext ctx, Creature target, decimal amount, ValueProp props, params object?[] extras)
    {
        var (dealer, card, play) = Split(extras);
#if CSK_GAME_111_OR_NEWER
        if (play != null || card != null)
            return CreatureCmd.Damage(ctx, target, amount, props, dealer, card, play);
        if (dealer != null)
            return CreatureCmd.Damage(ctx, target, amount, props, dealer);
        throw new InvalidOperationException("Damage 需要 dealer 或 cardSource");
#else
        if (card != null)
            return CreatureCmd.Damage(ctx, target, amount, props, dealer, card);
        if (dealer != null)
            return CreatureCmd.Damage(ctx, target, amount, props, dealer);
        throw new InvalidOperationException("0.107 Damage 需要 dealer 或 cardSource");
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext ctx, IEnumerable<Creature> targets, decimal amount, ValueProp props, params object?[] extras)
    {
        var (dealer, card, play) = Split(extras);
#if CSK_GAME_111_OR_NEWER
        if (play != null || card != null)
            return CreatureCmd.Damage(ctx, targets, amount, props, dealer, card, play);
        return CreatureCmd.Damage(ctx, targets, amount, props, dealer);
#else
        if (dealer != null)
            return CreatureCmd.Damage(ctx, targets, amount, props, dealer);
        if (card != null)
            return DamageEach(ctx, targets, amount, props, card);
        throw new InvalidOperationException("0.107 多目标 Damage 需要 dealer");
#endif
    }

    public static Task LoseBlock(PlayerChoiceContext ctx, Creature creature, decimal amount, Creature? source)
    {
#if CSK_GAME_111_OR_NEWER
        return CreatureCmd.LoseBlock(ctx, creature, amount, source);
#else
        return CreatureCmd.LoseBlock(creature, amount);
#endif
    }

#if !CSK_GAME_111_OR_NEWER
    private static async Task<IEnumerable<DamageResult>> DamageEach(
        PlayerChoiceContext ctx, IEnumerable<Creature> targets, DamageVar damageVar, CardModel card)
    {
        var results = new List<DamageResult>();
        foreach (var target in targets)
            results.AddRange(await CreatureCmd.Damage(ctx, target, damageVar, card));
        return results;
    }

    private static async Task<IEnumerable<DamageResult>> DamageEach(
        PlayerChoiceContext ctx, IEnumerable<Creature> targets, decimal amount, ValueProp props, CardModel card)
    {
        var results = new List<DamageResult>();
        foreach (var target in targets)
            results.AddRange(await CreatureCmd.Damage(ctx, target, amount, props, card));
        return results;
    }
#endif
}
