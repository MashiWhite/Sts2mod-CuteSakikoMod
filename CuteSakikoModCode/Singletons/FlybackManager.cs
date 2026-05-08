using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Utils;

// 引入 TimeWatch

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class FlybackManager : SingletonModel
{

    public event Action<int, int>? OnFlybackDataChanged;
    public override bool ShouldReceiveCombatHooks => false;
    public static FlybackManager Instance => ModelDb.Singleton<FlybackManager>();
    
    public static void InvalidatePlayerCache(Player player)
    {
        _tempPlayCounts.Remove(player);
    }

    // 全局总次数（所有玩家之和）
    public int TotalPlayCount
    {
        get
        {
            int total = 0;
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState != null)
            {
                foreach (var player in runState.Players)
                    total += GetPlayerPlayCount(player);
            }
            return total;
        }
    }

    // 兼容旧代码的属性（返回总和）
    public int PlayCount => TotalPlayCount;

    // 获取单个玩家的计数（优先遗物，其次临时）
    private static readonly Dictionary<Player, int> _tempPlayCounts = new();

// 获取单个玩家的计数（优先遗物，其次临时）
    private int GetPlayerPlayCount(Player player)
    {
        // 先检查临时字典
        if (_tempPlayCounts.TryGetValue(player, out var count))
            return count;

        // 如果没有临时记录，就从玩家身上找遗物
        var timeWatch = player.Relics.OfType<TimeWatch>().FirstOrDefault();
        if (timeWatch != null)
        {
            // 关键：直接从字段上读，并且缓存到字典里，避免循环调用
            int c = timeWatch.GetFlybackPlayCount();
            _tempPlayCounts[player] = c;  // 缓存住
            return c;
        }
        return 0;
    }

    // 增加指定玩家的计数
    public void IncrementPlayCountForPlayer(Player player)
    {
        if (player == null) return;

        var timeWatch = player.Relics.OfType<TimeWatch>().FirstOrDefault();
        if (timeWatch != null)
        {
            timeWatch.IncrementPlayCount();      // 直接增加遗物上的计数
        }
        else
        {
            // 没有遗物，暂存到内存
            _tempPlayCounts[player] = GetPlayerPlayCount(player) + 1;
        }
        NotifyDataChanged();
    }

    // 当玩家获得 TimeWatch 遗物时，将临时计数迁移到遗物中
    public static void TransferTempCountToTimeWatch(Player player, TimeWatch timeWatch)
    {
        if (player == null || timeWatch == null) return;
        if (_tempPlayCounts.TryGetValue(player, out int tempCount) && tempCount > 0)
        {
            // 把临时计数加到遗物已有计数上
            for (int i = 0; i < tempCount; i++)
                timeWatch.IncrementPlayCount();
            _tempPlayCounts.Remove(player);
        }
    }

    // 兼容旧代码的无参方法（单人模式取第一个玩家）
    public void IncrementPlayCount()
    {
        var player = GetCurrentPlayer();
        if (player != null)
            IncrementPlayCountForPlayer(player);
        else
            Log.Warn("FlybackManager: Cannot increment play count because no player found.");
    }

    private Player? GetCurrentPlayer()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        return runState?.Players.FirstOrDefault();
    }

    public static int GetReloadCount()
    {
        var field = typeof(RunManager).GetField("_numReloads", BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (int)field.GetValue(RunManager.Instance) : 0;
    }

    private void NotifyDataChanged()
    {
        OnFlybackDataChanged?.Invoke(TotalPlayCount, GetReloadCount());
    }
}