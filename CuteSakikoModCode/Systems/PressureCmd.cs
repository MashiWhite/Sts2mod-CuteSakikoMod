using CuteSakikoMod.CuteSakikoModCode.Others.Config;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class PressureCmd
{
    /// <summary>
    /// 施加压力。若配置开启多人缩放，且目标是敌人，则按玩家数 × 遭遇系数放大。
    /// </summary>
    public static async Task Apply(
        PlayerChoiceContext ctx,
        Creature target,
        decimal baseAmount,
        Creature? applier,
        CardModel? source)
    {
        if (target == null || baseAmount <= 0) return;

        var finalAmount = GetFinalAmount(target, baseAmount);
        if (finalAmount <= 0) return;

        await PowerCmd.Apply<PressurePower>(ctx, target, finalAmount, applier, source);
    }

    /// <summary>计算施加时实际使用的压力层数。</summary>
    public static int GetFinalAmount(Creature target, decimal baseAmount)
    {
        if (baseAmount <= 0) return 0;

        // 从 run snapshot 读，两端一致
        if (!IsScaleEnabledInCurrentRun()) return (int)baseAmount;

        if (target == null || !target.IsEnemy) return (int)baseAmount;

        var combatState = target.CombatState;
        if (combatState == null) return (int)baseAmount;

        var playerCount = combatState.Players.Count;
        if (playerCount <= 1) return (int)baseAmount;

        var scaling = MultiplayerScalingModel.GetMultiplayerScaling(
            combatState.Encounter, combatState.RunState.CurrentActIndex);

        return (int)Math.Ceiling(baseAmount * playerCount * scaling);
    }

    /// <summary>
    /// 从 run snapshot 读取缩放开关。两端读到同一个值，避免分歧。
    /// 单机 / 读不到时按"关闭"处理。
    /// </summary>
    private static bool IsScaleEnabledInCurrentRun()
    {
        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null) return false;

        var data = GameplayConfigSync.RunConfigSlot?.Get(state);
        return data?.ScalePressureInMultiplayer ?? false;
    }
}