using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

public sealed class AtkByMemoryPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // 只对玩家生效，且确保在战斗中
        if (Owner?.IsPlayer != true || Owner.Player == null) return;
        var combatState = Owner.CombatState;
        if (combatState == null) return;

        // 构造合法的 PlayerChoiceContext
        var ownerPlayer = combatState.Players.FirstOrDefault(p => p.Creature == Owner);
        if (ownerPlayer == null) return;
        var ctx = new HookPlayerChoiceContext(ownerPlayer, ownerPlayer.NetId, GameActionType.Combat);

        // 用记忆牌填满手牌
        await MemoryCmd.Recall(ctx, Owner.Player, allowChoose: false, fillHand: true, upgraded: false);
    }
}