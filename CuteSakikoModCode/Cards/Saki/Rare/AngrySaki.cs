using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class AngrySaki() : CuteSakikoModCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("NeedPressure", 8)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            // 存在任意可命中的敌人拥有压力达到阈值时，卡牌高亮
            var need = DynamicVars["NeedPressure"].IntValue;
            var combat = Owner?.Creature?.CombatState;
            if (combat == null) return false;
            return combat.HittableEnemies.Any(e =>
                e.GetPower<PressurePower>() is PressurePower p && p.Amount >= need);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var damage = DynamicVars.Damage.BaseValue;
        var extraHits = 0;

        // 检查目标身上的压力，计算额外攻击次数
        var targetPressure = cardPlay.Target.GetPower<PressurePower>();
        var needPressure = DynamicVars["NeedPressure"].IntValue;
        if (targetPressure != null && targetPressure.Amount >= needPressure)
        {
            extraHits = targetPressure.Amount / needPressure; // 每 N 层一次
            var consumeAmount = extraHits * needPressure; // 需要消耗的层数
            await PowerCmd.ModifyAmount(choiceContext, targetPressure, -consumeAmount, Owner.Creature, this);
        }

        var totalHits = 1 + extraHits;

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .WithHitCount(totalHits)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
