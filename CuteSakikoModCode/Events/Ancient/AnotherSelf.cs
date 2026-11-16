using CuteSakikoMod.CuteSakikoModCode.Relics.Event.AnotherSelf;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Events.Ancient;

[RegisterActAncient(typeof(Hive))]
public class AnotherSelf : CuteSakikoAncientEvent
{
    public override Color ButtonColor => new(0f, 0.09f, 0.2f, 0.75f);
    public override Color DialogueColor => new(0.39f, 0.64f, 0.75f);

    public override EventAssetProfile AssetProfile => new(
        BackgroundScenePath: "res://CuteSakikoMod/scenes/ancient/another_self.tscn");

    public override string? CustomMapIconPath => "res://CuteSakikoMod/images/ui/anotherself_icon.png";
    public override string? CustomMapIconOutlinePath => "res://CuteSakikoMod/images/ui/anotherself_icon_outline.png";
    public override string? CustomRunHistoryIconPath => "res://CuteSakikoMod/images/ui/anotherself_icon.png";

    public override string? CustomRunHistoryIconOutlinePath =>
        "res://CuteSakikoMod/images/ui/anotherself_icon_outline.png";

    // 定义三个池子
    private IReadOnlyList<EventOption> Pool1 => new[]
    {
        CreateModRelicOption<AvemujicaSakiko>(),
        CreateModRelicOption<CrychicSakiko>()
    };

    private IReadOnlyList<EventOption> Pool2 => new[]
    {
        CreateModRelicOption<AutoDrinkMachine>(),
        CreateModRelicOption<Maracas>()
    };

    private IReadOnlyList<EventOption> Pool3 => new[]
    {
        CreateModRelicOption<SandCastle>(),
        CreateModRelicOption<FallenFlower>()
    };

    // 所有可能的选项（用于调试/历史）
    public override IEnumerable<EventOption> AllPossibleOptions =>
        [.. Pool1, .. Pool2, .. Pool3];

    /// <summary>
    ///     闪局（Blitz）：Pool1 的两件都是「改地图」遗物，会把 BlitzActMap 换成 ScaledActMap
    ///     （NBlitzTopBarMap 会直接抛异常），所以闪局不走 Pool1。
    /// </summary>
    private bool IsBlitz
    {
        get
        {
#if STS2_AT_LEAST_0_113_0
            return Owner?.RunState.GameMode == GameMode.Blitz;
#else
            return false;
#endif
        }
    }

    // 生成初始选项：从每个池子中随机选一个；
    // 闪局时第一个选项改用 Pool3 里没被第三个选项抽走的那个遗物。
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var pool3 = Pool3;
        var thirdIndex = Rng.NextInt(pool3.Count);

        EventOption first;
        if (IsBlitz && pool3.Count > 1)
        {
            var remaining = new List<EventOption>(pool3.Count - 1);
            for (var i = 0; i < pool3.Count; i++)
                if (i != thirdIndex)
                    remaining.Add(pool3[i]);
            first = Rng.NextItem(remaining)!;
        }
        else
        {
            first = Rng.NextItem(Pool1)!;
        }

        return new[]
        {
            first,
            Rng.NextItem(Pool2)!,
            pool3[thirdIndex]
        };
    }
}