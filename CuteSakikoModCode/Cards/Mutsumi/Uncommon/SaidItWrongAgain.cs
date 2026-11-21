using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Uncommon;

/// <summary>
///     又说错话了（通用蓝卡）：1c 攻击，造成 10 点伤害；
///     若目标有格挡，则改为获得 5 层言刃，并打出一次 0 基础伤害的真实攻击
///     （吃言刃/力量/活力等攻击加成）。
///     升级后 13 点伤害 / 8 层言刃。
/// </summary>
public class SaidItWrongAgain()
    : CuteMutsumiCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new PowerVar<WordBladePower>(5m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WordBladePower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 有格挡时：先获得言刃，再打一次 0 基础伤害的真实攻击。
        // 0 伤害也会吃言刃 / 力量 / 活力等攻击加成，所以这里不能直接 return。
        if (cardPlay.Target.Block > 0m)
        {
            await PowerCmd.Apply<WordBladePower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["WordBladePower"].IntValue,
                Owner.Creature,
                this);

            await DamageCmd.Attack(0m)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["WordBladePower"].UpgradeValueBy(3m);
    }
}
