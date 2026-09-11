
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class NekoLeader : CuteRanaCard
{
    public NekoLeader() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<NekoLeaderPower>(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<NekoLeaderPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = DynamicVars["NekoLeaderPower"].BaseValue;
        // 施加 1 层能力（无论升级与否，效果不变）
        await PowerCmd.Apply<NekoLeaderPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["NekoLeaderPower"].UpgradeValueBy(1);
    }
}