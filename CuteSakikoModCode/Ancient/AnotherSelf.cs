
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Ancient;

[RegisterSharedAncient] // 使用共享，在IsAllowed中控制出现章节
public class AnotherSelf : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0f, 0.09f, 0.2f, 0.75f);
    public override Color DialogueColor => new(0.39f, 0.64f, 0.75f);

    public override EventAssetProfile AssetProfile => new(
        BackgroundScenePath: "res://CuteSakikoMod/scenes/Ancient/another_self.tscn");

    public override string? CustomMapIconPath => "res://CuteSakikoMod/images/ui/anotherself_icon.png";
    public override string? CustomMapIconOutlinePath => "res://CuteSakikoMod/images/ui/anotherself_icon_outline.png";
    public override string? CustomRunHistoryIconPath => "res://CuteSakikoMod/images/ui/anotherself_icon.png";

    public override string? CustomRunHistoryIconOutlinePath =>
        "res://CuteSakikoMod/images/ui/anotherself_icon_outline.png";

    // 定义三个池子
    private IReadOnlyList<EventOption> Pool1 => new[]
    {
        CreateModRelicOption<SandCastle>(),
        CreateModRelicOption<Anchor>()
    };

    private IReadOnlyList<EventOption> Pool2 => new[]
    {
        CreateModRelicOption<LizardTail>(),
        CreateModRelicOption<ArcaneScroll>()
    };

    private WeightedList<EventOption> Pool3 => new()
    {
        { CreateModRelicOption<YummyCookie>(), 2 },
        { CreateModRelicOption<WingCharm>(), 1 }
    };

    // 所有可能的选项（用于调试/历史）
    public override IEnumerable<EventOption> AllPossibleOptions =>
        Pool1.Concat(Pool2).Concat(Pool3);

    // 生成初始选项（从每个池子中随机选一个）
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[]
        {
            Rng.NextItem(Pool1)!,
            Rng.NextItem(Pool2)!,
            Pool3.GetRandom(Rng)
        };
    }

    // 出现条件：第二幕或第三幕（索引1和2）
    public override bool IsAllowed(IRunState runState)
    {
        var actIndex = runState.CurrentActIndex; // 0-based
        return actIndex == 1 || actIndex == 2;
    }
}

