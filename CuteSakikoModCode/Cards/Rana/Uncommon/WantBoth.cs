using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class WantBoth : CuteRanaCard
{
    public WantBoth() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<WantBothPower>();
        }
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WantBothPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}