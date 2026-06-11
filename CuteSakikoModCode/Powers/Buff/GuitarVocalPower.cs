using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class GuitarVocalPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加

    // 延迟结算相关字段
    private int _pendingDamage;
    private CancellationTokenSource? _pendingCts;

    public async Task OnNoteGained(int count)
    {
        var owner = Owner;
        if (owner?.CombatState == null) return;

        // 累加伤害
        _pendingDamage += count * Amount;

        // 取消之前的延迟任务
        _pendingCts?.Cancel();
        _pendingCts = new CancellationTokenSource();
        var cts = _pendingCts;

        try
        {
            // 等待 30 毫秒，让同一批次的音符都能被合并
            await Task.Delay(30, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // 被取消，说明又有新音符到来，直接返回
            return;
        }

        // 延迟结束且未被取消，执行一次总伤害
        var totalDamage = _pendingDamage;
        _pendingDamage = 0; // 清空

        var enemies = owner.CombatState.Enemies.Where(e => e.IsHittable).ToList();
        if (enemies.Count == 0) return;

        var rng = owner.CombatState.RunState.Rng.CombatCardSelection;
        var target = rng.NextItem(enemies);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target,
            totalDamage, ValueProp.Unpowered, Owner, null);
    }

    // 可选：战斗结束时清空状态，防止残留
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        _pendingCts?.Cancel();
        _pendingDamage = 0;
        await base.AfterCombatEnd(room);
    }
}