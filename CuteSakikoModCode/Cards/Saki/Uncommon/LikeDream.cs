using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
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
        // 获取当前压力层数
        var pressure = Owner.Creature.GetPower<PressurePower>();
        var currentAmount = pressure?.Amount ?? 0;
        if (currentAmount > 0)
        {
            // 记录翻倍前的崩溃层数
            var breakDown = Owner.Creature.GetPower<BreakDownPower>();
            var previousBreakDownAmount = breakDown?.Amount ?? 0;

            // 翻倍：增加相同数量的层数
            await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, currentAmount, Owner.Creature, this);

            // 检查崩溃层数是否增加（即触发了崩溃）
            breakDown = Owner.Creature.GetPower<BreakDownPower>();
            var newBreakDownAmount = breakDown?.Amount ?? 0;
            if (newBreakDownAmount > previousBreakDownAmount)
            {
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
                await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}