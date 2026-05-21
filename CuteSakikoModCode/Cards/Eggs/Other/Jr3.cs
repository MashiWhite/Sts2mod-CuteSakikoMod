using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

public class Jr3() : ModTokenCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar("lowDamage",11m, ValueProp.Move); // 伤
            yield return new DamageVar(12m, ValueProp.Move); // 高伤
            yield return new RepeatVar(1); // 次数
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var lowDamage = DynamicVars["lowDamage"].IntValue;
        // 低伤一次
        await DamageCmd.Attack(lowDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 高伤一次
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["lowDamage"].UpgradeValueBy(4);
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}