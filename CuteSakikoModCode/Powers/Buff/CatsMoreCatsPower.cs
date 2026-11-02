using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     猫咪！还有猫咪！：你每打出一张猫咪，抽 N 张牌（N = 层数）。
///     可叠层（Counter）：拿到第二张同名卡时层数叠加，抽牌数随之增加。
/// </summary>
public sealed class CatsMoreCatsPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword()); }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (!cardPlay.Card.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword())) return;

        var draw = Amount;
        if (draw <= 0) return;

        Flash();
        await CardPileCmd.Draw(choiceContext, draw, Owner.Player);
    }
}
