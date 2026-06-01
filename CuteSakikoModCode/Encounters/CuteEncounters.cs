using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters;

public abstract class CuteEncounters : ModEncounterTemplate
{
    // 重写 IsValidForAct，根据配置决定是否允许该遭遇在指定 Act 中生成
    public override bool IsValidForAct(ActModel act)
    {
        // 读取配置（通过 ModConfig 辅助类，见下方）
        return ModConfig.EnableModMonsters;
    }
}