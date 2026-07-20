using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Content;
using CuteSakikoMod.CuteSakikoModCode.Others;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

/// <summary>
/// 所有自定义普通事件的抽象基类，受设置“Custom Events”开关控制。
/// 子类可以重写 IsAllowedInternal 来追加额外的生成条件。
/// </summary>
public abstract class CuteSakikoEvent : ModEventTemplate
{
    public sealed override bool IsAllowed(IRunState runState)
    {
        if (!ModConfig.EnableCustomEvents)
            return false;
        return IsAllowedInternal(runState);
    }

    /// <summary>
    /// 子类可重写此方法，添加除全局开关外的自定义出现条件。
    /// 默认返回 true。
    /// </summary>
    protected virtual bool IsAllowedInternal(IRunState runState) => true;
}