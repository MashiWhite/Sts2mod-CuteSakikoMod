using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class MarksOfPractice() : CuteAnonCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<MarksOfPracticePower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var stacks = IsUpgraded ? 2 : 1;
        await PowerCmd.Apply<MarksOfPracticePower>(choiceContext, Owner.Creature, stacks, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 效果通过 IsUpgraded 自动处理
    }
}