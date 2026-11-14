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
/// 0.107：Damage 最多 dealer + cardSource；
/// 0.111+：支持 dealer + cardSource + cardPlay。
/// ★ 两版都有「dealer / cardSource 均可为 null」的重载；事件伤害（如暗巷影猫的离开）
///   就是 dealer = null + cardSource = null，调用点统一走这里，避免每个文件里写 #if。
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
        return CreatureCmd.Damage(ctx, target, damageVar, dealer, card, play);
#else
        return CreatureCmd.Damage(ctx, target, damageVar, dealer, card);
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext ctx, IEnumerable<Creature> targets, DamageVar damageVar, params object?[] extras)
    {
        var (dealer, card, play) = Split(extras);
#if CSK_GAME_111_OR_NEWER
        return CreatureCmd.Damage(ctx, targets, damageVar, dealer, card, play);
#else
        return CreatureCmd.Damage(ctx, targets, damageVar, dealer, card);
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext ctx, Creature target, decimal amount, ValueProp props, params object?[] extras)
    {
        var (dealer, card, play) = Split(extras);
#if CSK_GAME_111_OR_NEWER
        return CreatureCmd.Damage(ctx, target, amount, props, dealer, card, play);
#else
        return CreatureCmd.Damage(ctx, target, amount, props, dealer, card);
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext ctx, IEnumerable<Creature> targets, decimal amount, ValueProp props, params object?[] extras)
    {
        var (dealer, card, play) = Split(extras);
#if CSK_GAME_111_OR_NEWER
        return CreatureCmd.Damage(ctx, targets, amount, props, dealer, card, play);
#else
        return CreatureCmd.Damage(ctx, targets, amount, props, dealer, card);
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
}
