using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class HighHigh() : CustomCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<ShrinkPower>();
        }
    }

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (CombatState == null) return false;
            var selfPressure = Owner.Creature.GetPower<PressurePower>()?.Amount ?? 0;
            foreach (var enemy in CombatState.HittableEnemies)
            {
                var enemyPressure = enemy.GetPower<PressurePower>()?.Amount ?? 0;
                if (enemyPressure > selfPressure) return true;
            }

            return false;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // 造成伤害
        var damage = DynamicVars["Damage"].IntValue;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(target)
            .Execute(choiceContext);

        // 获取自身和目标压力层数
        var selfPressure = Owner.Creature.GetPower<PressurePower>()?.Amount ?? 0;
        var targetPressure = target.GetPower<PressurePower>()?.Amount ?? 0;

        // 若目标压力大于自己，给予缩小效果（升级后2层，否则1层）
        if (targetPressure > selfPressure)
        {
            var shrinkAmount = IsUpgraded ? 2 : 1;
            await PowerCmd.Apply<ShrinkPower>(target, shrinkAmount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害 8 → 13
        DynamicVars["Damage"].UpgradeValueBy(5m);
    }
}