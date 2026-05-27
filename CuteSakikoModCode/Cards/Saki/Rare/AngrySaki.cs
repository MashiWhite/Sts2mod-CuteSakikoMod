
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
        new DamageVar(10m, ValueProp.Move)
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
            // 存在任意可命中的敌人拥有至少 10 层压力时，卡牌高亮
            var combat = Owner?.Creature?.CombatState;
            if (combat == null) return false;
            return combat.HittableEnemies.Any(e =>
                e.GetPower<PressurePower>() is PressurePower p && p.Amount >= 10);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var damage = DynamicVars.Damage.BaseValue;
        int extraHits = 0;

        // 检查目标身上的压力是否足够 10 层
        var targetPressure = cardPlay.Target.GetPower<PressurePower>();
        if (targetPressure != null && targetPressure.Amount >= 10)
        {
            extraHits = 1;
            await PowerCmd.ModifyAmount(choiceContext, targetPressure, -10, Owner.Creature, this);
        }

        int totalHits = 1 + extraHits;

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .WithHitCount(totalHits)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m); // 10 → 15
    }
}