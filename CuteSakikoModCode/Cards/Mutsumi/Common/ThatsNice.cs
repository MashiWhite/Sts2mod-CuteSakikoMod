using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Common;

/// <summary>
///     真好呢（通用白卡）：0c 攻击，获得 1 层言刃，对自己造成 1 点伤害，
///     再造成 1 点伤害 3 次；升级后言刃 2 层、攻击 4 次。
/// </summary>
public class ThatsNice() : CuteMutsumiCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new RepeatVar(3),
        new PowerVar<WordBladePower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WordBladePower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        await PowerCmd.Apply<WordBladePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["WordBladePower"].IntValue,
            Owner.Creature,
            this);

        // 自伤也是一次真实攻击，走 AttackCommand，让 AfterAttack 自动吃言刃。
        // 言刃的扣除现在延迟到整张牌结算完，两次 AttackCommand 自动共享同一层数。
        await DamageCmd.Attack(1m)
            .FromCard(this, cardPlay)
            .Targeting(Owner.Creature)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (Owner.Creature.IsDead) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars["Repeat"].IntValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WordBladePower"].UpgradeValueBy(1m);
        DynamicVars["Repeat"].UpgradeValueBy(1m);
    }
}
