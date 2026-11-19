using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     言刃：下一次攻击牌结算时，对每个被命中的目标按段数各造成「层数」等量的固定真实伤害，随后清空。
///     固定真实伤害走 ValueProp.Unblockable | Unpowered + ModifyDamageHookType.None：
///     不吃格挡、力量、虚弱、易伤，也不吃任何限伤/减伤（含 HardToKill、HardenedShell）。
/// </summary>
public sealed class WordBladePower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    /// <summary>最近一次攻击消耗的层数，供「咄咄逼人」返还使用。</summary>
    public int LastConsumed { get; private set; }

    /// <summary>
    ///     同一张攻击牌内可能有多个 AttackCommand（例如先打自己再打敌人）。
    ///     第一次攻击时记录这张牌使用的层数，之后所有攻击共用；
    ///     真正的扣除延迟到 AfterCardPlayed，整张牌结算完后只扣一次。
    /// </summary>
    private readonly Dictionary<CardModel, int> _pendingCardStacks = new();

    public void ResetLastConsumed() => LastConsumed = 0;

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (Owner == null || Owner.IsDead) return;
        if (command.Attacker != Owner) return;
        if (command.ModelSource is not CardModel card) return;
        if (Amount <= 0) return;

        var stacks = Amount;
        if (_pendingCardStacks.TryGetValue(card, out var pending))
            stacks = pending;
        else
            _pendingCardStacks[card] = stacks;

        LastConsumed = stacks;

        foreach (var hit in command.Results)
        {
            foreach (var result in hit)
            {
                var target = result.Receiver;
                if (target == null || target.IsDead) continue;

                // 用 AsyncLocal 作用域放行「坚固外壳」，不污染 ValueProp。
                var props = ValueProp.Unblockable | ValueProp.Unpowered;
                using var _ = WordBladeDamageScope.Enter();
#if STS2_AT_LEAST_0_113_0
                await CreatureCmd.Damage(
                    choiceContext,
                    new[] { target },
                    stacks,
                    props,
                    null,
                    null,
                    null,
                    ModifyDamageHookType.None);
#elif CSK_GAME_111_OR_NEWER
                await CreatureCmd.Damage(
                    choiceContext,
                    new[] { target },
                    stacks,
                    props,
                    null,
                    null,
                    null);
#else
                await CreatureCmd.Damage(
                    choiceContext,
                    new[] { target },
                    stacks,
                    props,
                    null,
                    null);
#endif
            }
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay == null || Owner == null) return;
        if (!_pendingCardStacks.Remove(cardPlay.Card, out var stacks)) return;
        if (stacks <= 0 || Amount <= 0) return;

        await PowerCmd.ModifyAmount(choiceContext, this, -stacks, Owner, null);
    }
}
