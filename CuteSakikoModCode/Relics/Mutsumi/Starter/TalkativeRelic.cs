using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Mutsumi.Starter;

/// <summary>
///     多言（Mortis 初始遗物）：每打出一张攻击牌，获得 1 层「言刃」。
/// </summary>
public class TalkativeRelic : CuteMutsumiRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<WordBladePower>()];

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (Owner == null) return;
        if (command.Attacker != Owner.Creature) return;
        if (command.ModelSource is not CardModel) return;

        await PowerCmd.Apply<WordBladePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null);
        Flash();
    }
}
