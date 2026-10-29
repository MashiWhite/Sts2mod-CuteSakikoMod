using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class MarksOfPractice() : CuteAnonCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MarksPower", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<MarksOfPracticePower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.EquippedChords.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var stacks = DynamicVars["MarksPower"].IntValue;
        await PowerCmd.Apply<MarksOfPracticePower>(choiceContext, Owner.Creature, stacks, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MarksPower"].UpgradeValueBy(1m);
        // 效果通过 IsUpgraded 自动处理
    }
}