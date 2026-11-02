using CuteSakikoMod.CuteSakikoModCode.Others.Config;
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

    public sealed override bool IsAllowed(IRunState runState)
    {
        if (!ModConfig.EnableCustomEvents)
            return false;

        var typeEnabled = NeedsModCharacter
            ? ModConfig.EnableCharacterLockedEvents
            : ModConfig.EnablePlainEvents;
        if (!typeEnabled)
            return false;

        return IsAllowedInternal(runState);
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
