using CuteSakikoMod.CuteSakikoModCode.Others.Config;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

/// <summary>
///     所有自定义普通事件的抽象基类，受设置中的三层开关控制：
///     总开关 EnableCustomEvents → 分类子开关（按 NeedsModCharacter 分两组）→ 事件自身条件。
///     子类必须重写 NeedsModCharacter 声明自己属于哪一类，并可重写 IsAllowedInternal 追加额外生成条件。
/// </summary>
public abstract class CuteSakikoEvent : ModEventTemplate
{
    /// <summary>
    ///     本事件是否需要 mod 角色才能解锁全部选项。
    ///     true  → 受“需要 mod 角色的事件”开关控制（如沙滩、情感二重奏、错位）。
    ///     false → 受“普通事件”开关控制（如金狗、炒虾、暗巷影猫）。
    ///     ★ 故意声明为 abstract：新事件必须显式表态，漏写会直接编译失败，
    ///       不会像“继承错基类”那样静默按错误开关生成。
    /// </summary>
    protected abstract bool NeedsModCharacter { get; }

    /// <summary>
    ///     本事件的锁定选项是否「刚需多人才能解锁」——即解锁条件要求
    ///     **至少两名玩家**（当前=两名特定 mod 角色；日后做更多角色/更多玩家数时，
    ///     凡"单人无论如何凑不齐解锁条件"的事件都算这一类）。
    ///     true  → 受「多人事件」三态设置控制：关闭=不出现 / 仅多人=只在多人跑局出现（默认）/
    ///             开启=单人也出现（锁定项照常显示锁定态）。
    ///     false → 不是多人刚需事件（缺一个角色单人也能解锁），不受该设置影响（默认）。
    ///     判定与 IsAllowedInternal 一样禁止调用 RNG（生成期反复调用）。
    /// </summary>
    protected virtual bool RequiresMultiplayerToUnlock => false;

    public sealed override bool IsAllowed(IRunState runState)
    {
        if (!ModConfig.EnableCustomEvents)
            return false;

        var typeEnabled = NeedsModCharacter
            ? ModConfig.EnableCharacterLockedEvents
            : ModConfig.EnablePlainEvents;
        if (!typeEnabled)
            return false;

        // ★ 多人事件三态（2026-11-03 用户定义）：关闭=从不出现；仅多人=单人模式不生成；
        //   开启=单人也出现。多人跑局下恒出现（三态只约束单人）。
        //   判定用 runState 而非 RunManager.Instance：生成期各端必须用同一输入。
        if (RequiresMultiplayerToUnlock && !IsMultiplayerRun(runState))
        {
            var mode = ModConfig.MultiplayerEventMode;
            if (mode == MultiplayerEventMode.Off || mode == MultiplayerEventMode.MultiplayerOnly)
                return false;
        }

        return IsAllowedInternal(runState);
    }

    /// <summary>单人=false / 多人=true。只看玩家数：多人跑局 Players 恒 ≥ 2，且各端一致。</summary>
    private static bool IsMultiplayerRun(IRunState runState)
        => runState?.Players != null && runState.Players.Count >= 2;

    /// <summary>
    ///     该玩家当前是否被 DirectConnectIP（第三方直连 mod）的"离线托管"接管。
    ///
    ///     背景（2026-11-08 修复多人卡死）：DirectConnectIP 的
    ///     AutoPassRemoteChoiceForGhostsPatch 对托管玩家的所有远程选择
    ///     一律回 `PlayerChoiceResult.FromIndex(0)`（硬编码 Index 类型）。
    ///     于是事件里任何"从牌组选牌"（CardSelectCmd.FromDeck*）都会在
    ///     CardSelectCmd 内部 `WaitForRemoteChoice(...).AsDeckCards()` 抛
    ///     InvalidOperationException → 事件状态机卡死。
    ///
    ///     兼容方式：反射调用它的公开判定 API，**对托管玩家跳过选牌**。
    ///     ⚠️ 完全松耦合：
    ///       · 未安装该 mod → Type.GetType 返回 null（不抛异常）→ 恒 false
    ///       · 该 mod 改版删了方法 → GetMethod 返回 null → 恒 false
    ///       · 任何异常都被 catch → 恒 false
    ///     恒 false 意味着走原本的正常路径，**对没装该 mod 的玩家零影响**。
    /// </summary>
    protected static bool IsTakenOverByDirectConnect(Player? player)
    {
        if (player == null) return false;
        try
        {
            var type = Type.GetType(
                "DirectConnectIP.Patches.Network.OfflineTakeoverCore, DirectConnectIP");
            if (type == null) return false;

            var method = type.GetMethod(
                "IsGhost",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null) return false;

            return method.Invoke(null, new object[] { player.NetId }) is true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     子类可重写此方法，添加除全局开关外的自定义出现条件。
    ///     ⚠️ 禁止在此方法内调用任何 RNG（state.Rng.*）：本方法会在生成期被反复调用，
    ///     一旦消耗随机数，配置不同的客户端之间随机序列就会分歧，破坏联机一致性。
    ///     默认返回 true。
    /// </summary>
    protected virtual bool IsAllowedInternal(IRunState runState)
    {
        return true;
    }
}
