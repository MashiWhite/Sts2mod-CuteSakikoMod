using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Rare;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class LikeDream() : CuteSakikoModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pressure = Owner.Creature.GetPower<PressurePower>();
        var currentAmount = pressure?.Amount ?? 0;
        if (currentAmount <= 0) return;

        // 有面具时崩溃会被移除，需用另一套判断
        var hasMask = Owner.Relics.OfType<Mask>().Any();
        var preHp = Owner.Creature.CurrentHp;

        // 翻倍前记录崩溃层数
        var previousBreakDownAmount = Owner.Creature.GetPower<BreakDownPower>()?.Amount ?? 0;

        // 翻倍
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, currentAmount, Owner.Creature, this);

        bool triggeredCollapse;
        if (hasMask)
        {
            // 有面具：崩溃不会出现，用"翻倍后压力 >= 当前生命值"判定本该触发崩溃
            triggeredCollapse = currentAmount * 2 >= preHp;
        }
        else
        {
            var newBreakDownAmount = Owner.Creature.GetPower<BreakDownPower>()?.Amount ?? 0;
            triggeredCollapse = newBreakDownAmount > previousBreakDownAmount;
        }

        if (triggeredCollapse)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}