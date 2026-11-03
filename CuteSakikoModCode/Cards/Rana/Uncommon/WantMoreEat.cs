using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

/// <summary>
///     我还要吃：每回合你第一次食用抹茶芭菲时，抽 N 张牌（N = 层数）。
///     升级：N 1 → 2。
/// </summary>
public class WantMoreEat : CuteRanaCard
{
    public WantMoreEat() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawOnEat", 1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WantMoreParfaitPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var layers = DynamicVars["DrawOnEat"].BaseValue;
        await PowerCmd.Apply<WantMoreParfaitPower>(choiceContext, Owner.Creature, layers, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DrawOnEat"].UpgradeValueBy(1m); // 1 → 2
    }
}
