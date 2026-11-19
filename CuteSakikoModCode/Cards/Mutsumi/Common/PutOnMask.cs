using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Common;

/// <summary>
///     戴上面具（通用白卡）：0c 技能，获得 6 点格挡；
///     下回合开始前若格挡耗尽，受到 3 点伤害。升级后 7 格挡 / 1 伤害。
/// </summary>
public class PutOnMask() : CuteMutsumiCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new PowerVar<MaskBlockWatchPower>(3m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<MaskBlockWatchPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<MaskBlockWatchPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MaskBlockWatchPower"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars["MaskBlockWatchPower"].UpgradeValueBy(-2m);
    }
}
