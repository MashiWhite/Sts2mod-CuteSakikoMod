
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class DontRushMe : CuteRanaCard
{
    public DontRushMe() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 基础伤害 7，每保留 1 回合额外增加 3 点（升级后 6 点）
            yield return new CalculationBaseVar(7m);
            yield return new ExtraDamageVar(3m);
            // 记录本场战斗此牌已保留的回合数
            yield return new DynamicVar("RetainTurns", 0m);
            // 伤害 = 基础 + 额外 × 保留回合数
            yield return new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((card, _) => card.DynamicVars["RetainTurns"].BaseValue);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 玩家回合结束时，若此牌仍在手牌中，则保留回合数 +1
        if (side == CombatSide.Player && Pile?.Type == PileType.Hand)
        {
            DynamicVars["RetainTurns"].BaseValue += 1;
        }
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        // 升级后每回合增幅由 3 变为 6
        DynamicVars["ExtraDamage"].UpgradeValueBy(3m);
    }
}