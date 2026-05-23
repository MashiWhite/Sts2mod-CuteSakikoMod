using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Memory;

public class StarPlay() : SakiMemoryCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    // 只保留一个 BlockVar，删掉 BlockNextTurn 和 PowerVar
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡，并捕获实际获得的格挡值（已受敏捷等加成）
        Decimal actualBlock = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 将实际获得的格挡值作为 Power 的层数
        await PowerCmd.Apply<BlockNextTurnPower>(choiceContext, Owner.Creature, actualBlock, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}