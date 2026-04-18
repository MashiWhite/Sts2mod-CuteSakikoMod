using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;

[Pool(typeof(TokenCardPool))]
public class GoldOption() : CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    // 消耗关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 动态变量：金币数量（基础30，升级40）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new("Gold", IsUpgraded ? 45m : 35m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        // 升级时金币数值在构造时已根据 IsUpgraded 决定，无需额外代码
    }
}