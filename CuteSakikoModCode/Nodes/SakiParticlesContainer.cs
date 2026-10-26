using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class SakiParticlesContainer : NParticlesContainer
{
    public override void _Ready()
    {
        base._Ready();

        // NParticlesContainer 没有公开的 Clear API，只能反射把内部数组换掉。
        // 包一层：反射失败（游戏改字段名）时退回 SetEmitting(false)，至少别让粒子继续喷。
        try
        {
            Traverse.Create(this).Field("_particles").SetValue(new Array<GpuParticles2D>());
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[SakiParticles] 清空 _particles 失败，退回 SetEmitting(false): {ex.Message}");
            try { SetEmitting(false); } catch { /* 兜底失败就保持原样 */ }
        }
    }
}