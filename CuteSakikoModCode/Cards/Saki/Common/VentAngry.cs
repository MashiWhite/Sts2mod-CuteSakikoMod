using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class VentAngry() : CuteSakikoModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    // 当存在至少一个敌人意图不是攻击时，卡牌高亮金色
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (CombatState == null)
                return false;
            // 检查是否存在任意可攻击的敌人，且其意图不是攻击（即不打算攻击）
            return CombatState.HittableEnemies.Any(e => e.Monster != null && !e.Monster.IntendsToAttack);
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 返回压力能力的悬停提示
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            // 如果有其他提示，继续 yield return
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 若敌人意图不是攻击，则抽牌并施加压力
        if (cardPlay.Target.Monster != null && !cardPlay.Target.Monster.IntendsToAttack)
        {
            var drawCount = IsUpgraded ? 2 : 1;
            var pressureAmount = IsUpgraded ? 10 : 8;

            // 抽牌
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);

            // 给敌人施加压力
            await PowerCmd.Apply<PressurePower>(choiceContext, cardPlay.Target, pressureAmount, Owner.Creature, this);
        }
    }


    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}