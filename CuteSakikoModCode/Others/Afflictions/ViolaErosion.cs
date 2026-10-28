using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Others.Afflictions;

/// <summary>
///     欺诈之蛇的"无法辨认"侵蚀状态。
///     真正的显示替换（卡图 / 标题 / 描述）由 Patches/ViolaErosionPatches 完成，
///     本类只负责标记状态与提供额外的卡面说明。
/// </summary>
[RegisterAffliction]
public sealed class ViolaErosion : AfflictionModel
{
    public override bool HasExtraCardText => true;
    public override bool IsStackable => false;
}
