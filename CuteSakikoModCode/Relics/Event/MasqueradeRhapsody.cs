using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class MasqueradeRhapsody : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BreakDownPower>();yield return HoverTipFactory.FromPower<PressurePower>(); }
    }
}