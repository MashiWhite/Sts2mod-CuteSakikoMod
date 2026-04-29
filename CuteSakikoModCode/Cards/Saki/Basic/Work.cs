using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;

[RegisterCharacterStarterCard(typeof(CuteSaki))]
public class Work() : CuteSakikoModCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    // 始终添加消耗关键词（基础版本）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new("Gold", 5m),
        new PowerVar<PressurePower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 返回压力能力的悬停提示
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            // 如果有其他提示，继续 yield return
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, Owner);
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, DynamicVars["PressurePower"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        // 升级后移除消耗关键词
        RemoveKeyword(CardKeyword.Exhaust);
    }
}