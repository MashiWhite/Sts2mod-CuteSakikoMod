using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class RainfallOmen : CuteRanaCard
{
    public RainfallOmen() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<RainfallOmenPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 给予自身 5 层雨前征兆能力（层数即每次触发获得的格挡值）
        await PowerCmd.Apply<RainfallOmenPower>(
            choiceContext,
            Owner.Creature,
            5,
            Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        // 费用 2 → 1
        EnergyCost.UpgradeBy(-1);
    }
}