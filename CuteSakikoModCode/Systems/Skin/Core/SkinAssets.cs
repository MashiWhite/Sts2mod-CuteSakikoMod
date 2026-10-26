namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;

public sealed record SkinAssets(
    string VisualsPath,
    string EnergyCounterPath,
    string MerchantPath,
    string RestSitePath,
    string IconTexturePath,
    string IconOutlineTexturePath,
    string IconPath,
    string TrailPath,
    string ArmPointingPath,
    string ArmRockPath,
    string ArmPaperPath,
    string ArmScissorsPath,
    // UsesAnimationPlayerStateMachine：
    //   true（默认）→ 该皮肤走 AnimationPlayer 状态机
    //   false        → 走原版 Spine 路径（saki 皮肤用）
    bool UsesAnimationPlayerStateMachine = true);