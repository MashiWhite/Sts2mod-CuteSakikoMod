using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

/// <summary>
///     挑食：弃掉手牌中所有与芭菲无关的牌，每弃 1 张抽 1 张牌。
///     升级：丢弃改为消耗。
/// </summary>
public class PickyEater : CuteRanaCard
{
    public PickyEater() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword()); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var parfaitKeyword = CutesakiKeywords.Parfait.GetModCardKeyword();
        var hand = PileType.Hand.GetPile(Owner)?
            .Cards.Where(c => c != cardPlay.Card)
            .ToList() ?? [];

        var toDiscard = hand
            .Where(c => !c.Keywords.Contains(parfaitKeyword))
            .ToList();

        foreach (var card in toDiscard)
        {
            if (IsUpgraded)
                await CardCmd.Exhaust(choiceContext, card);
            else
                await CardPileCmd.Add([card], PileType.Discard);
        }

        var n = toDiscard.Count;
        if (n > 0)
            await CardPileCmd.Draw(choiceContext, n, Owner);
    }
}
