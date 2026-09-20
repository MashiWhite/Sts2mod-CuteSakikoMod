using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class SakiSweep() : CuteSakikoModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 带上剑关键词，让 SwordManager.AfterCardPlayed 统一检测/补剑
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Sword.GetModCardKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PressurePower>(5m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<KnightSword>();
            yield return HoverTipFactory.FromPower<SakiSweepPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施加压力和横扫能力（补剑交给 SwordManager.AfterCardPlayed）
        var pressureAmount = DynamicVars["PressurePower"].IntValue;
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, pressureAmount, Owner.Creature, this);
        await PowerCmd.Apply<SakiSweepPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PressurePower"].UpgradeValueBy(5);
    }
}