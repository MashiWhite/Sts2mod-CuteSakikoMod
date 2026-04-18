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
        Traverse.Create(this).Field("_particles").SetValue(new Array<GpuParticles2D>());
    }
}