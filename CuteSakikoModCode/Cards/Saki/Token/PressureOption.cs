using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;

[Pool(typeof(TokenCardPool))]
public class PressureOption() : CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    // 消耗关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 动态变量：压力层数和力量值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PressurePower>(IsUpgraded ? 8m : 5m),
        new PowerVar<StrengthPower>(IsUpgraded ? 5m : 3m)
    ];

    // 悬停提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得压力
        await PowerCmd.Apply<PressurePower>(Owner.Creature, DynamicVars["PressurePower"].IntValue, Owner.Creature,
            this);
        // 获得力量
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars["StrengthPower"].IntValue, Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        // 升级数值已在构造时根据 IsUpgraded 决定
    }
}