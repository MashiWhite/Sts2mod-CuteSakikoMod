using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class RelicTransfer
{
    // 把同一个遗物实例从一个玩家转到另一个玩家，保留其内部状态（会触发 AfterObtained）
    public static async Task Transfer(RelicModel relic, Player target)
    {
        if (relic.Owner == target) return;
        await RelicCmd.Remove(relic);
        ClearOwner(relic);
        await RelicCmd.Obtain(relic, target);
    }

    // 只移除 + 清 owner，不立即给新玩家。
    public static async Task RemoveAndDetach(RelicModel relic)
    {
        await RelicCmd.Remove(relic);
        ClearOwner(relic);
    }

    // ★ 交换专用：添加到玩家但不触发 AfterObtained，避免拾取效果重复触发
    public static Task AttachWithoutObtained(RelicModel relic, Player target)
    {
        relic.AssertMutable();
        var runState = target.RunState;

        // 与 RelicCmd.Obtain 相同的注册流程，仅去掉最后的 AfterObtained()。
        // 历史记录 / 抓宝袋 / UI 都是"锦上添花"的步骤：单独兜住，任何一步失败都不影响遗物真正挂到玩家身上。
        TryRun(() =>
        {
            runState.CurrentMapPointHistoryEntry?
                .GetEntry(target.NetId).RelicChoices
                .Add(new ModelChoiceHistoryEntry(relic.Id, true));
        }, "记录遗物选择历史");

        target.AddRelicInternal(relic, -1);

        TryRun(() =>
        {
            if (relic.IsStackable) return;
            target.RelicGrabBag.Remove(relic);
            runState.SharedRelicGrabBag.Remove(relic);
        }, "从抓宝袋移除");

        TryRun(() =>
        {
            if (!LocalContext.IsMe(target)) return;
            NRun.Instance?.GlobalUi.RelicInventory.AnimateRelic(relic);
            NDebugAudioManager.Instance?.Play("relic_get.mp3");
            SaveManager.Instance.MarkRelicAsSeen(relic);
        }, "遗物 UI 动画");

        TryRun(() => relic.FloorAddedToDeck = runState.TotalFloor, "记录获得楼层");

        return Task.CompletedTask;
    }

    private static void TryRun(Action action, string step)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[RelicTransfer] {step} 失败（不影响转移本身）: {ex.Message}");
        }
    }

    /// <summary>
    ///     清空遗物的 owner。游戏 <c>RelicModel.Owner</c> 的 setter 在"已有 owner 且换人"时会抛
    ///     "Cannot move relic from one owner to another"，而 RemoveInternal 只置 HasBeenRemovedFromState、
    ///     不清 owner，所以只能反射置 null。失败时打 Warn，后续 Obtain 会因此抛错，便于尽早暴露版本变动。
    /// </summary>
    private static void ClearOwner(RelicModel relic)
    {
        try
        {
            _ownerField ??= typeof(RelicModel).GetField("_owner",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (_ownerField == null)
            {
                Entry.Logger.Warn("[RelicTransfer] 找不到 RelicModel._owner 字段，遗物转移会失败（游戏字段可能改名）");
                return;
            }

            _ownerField.SetValue(relic, null);
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[RelicTransfer] 清空 relic.Owner 失败: {ex.Message}");
        }
    }

    private static FieldInfo? _ownerField;
}