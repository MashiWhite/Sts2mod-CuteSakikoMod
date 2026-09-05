using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class DaggersDrawnPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加层数
    public override bool AllowNegative => false;

    // 每回合开始时触发（能力拥有者的回合开始）
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        // 只在自己的回合开始时触发
        if (player != Owner.Player) return;

        var amount = Amount;
        if (amount <= 0) return;

        // 只对所有敌人施加压力
        var enemies = CombatState.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        foreach (var enemy in enemies)
            await PowerCmd.Apply<PressurePower>(choiceContext, enemy, amount, Owner, null);
    }
}