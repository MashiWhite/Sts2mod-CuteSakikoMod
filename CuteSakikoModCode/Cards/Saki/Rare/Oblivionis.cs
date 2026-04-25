
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;


public class Oblivionis() : CuteSakikoModCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{

    // 动态变量：能力层数（基础1层，升级后无变化，但保留用于描述）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<OblivionisPower>(1m)
    ];

    // 悬停提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<OblivionisPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施加能力，固定1层（无层数变化，仅用于触发）
        await PowerCmd.Apply<OblivionisPower>(choiceContext,Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：费用减少1（2→1）
        EnergyCost.UpgradeBy(-1);
    }
}