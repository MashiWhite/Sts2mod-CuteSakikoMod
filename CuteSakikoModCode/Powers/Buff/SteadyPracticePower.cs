using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class SteadyPracticePower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single; // 不可叠层

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // 只在玩家回合结束时触发
        if (side != CombatSide.Player) return;

        var player = Owner?.Player;
        if (player == null) return;

        // 获取吉他遗物并演奏最新储存的和弦
        var guitar = player.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
            await guitar.TriggerLastStoredChord(choiceContext);
    }
}