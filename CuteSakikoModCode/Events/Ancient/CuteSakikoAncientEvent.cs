using MegaCrit.Sts2.Core.Models.Acts;
using STS2RitsuLib.Scaffolding.Content;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Events.Ancient;

/// <summary>
/// 所有自定义远古事件的抽象基类，受设置“自定义远古事件”开关控制。
/// </summary>
public abstract class CuteSakikoAncientEvent : ModAncientEventTemplate
{
    public override bool IsValidForAct(ActModel act)
    {
        // 设置关闭时，阻止该远古事件在任何章节生成
        if (!ModConfig.EnableCustomAncients)
            return false;
        return base.IsValidForAct(act);
    }
}