using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Ancient;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Ancient;

public class AnotherSelf : CustomAncientModel
{
    // 选项按钮颜色
    public override Color ButtonColor => new(0f, 0.09f, 0.2f, 0.75f);

    // 对话框颜色
    public override Color DialogueColor => new(0.39f, 0.64f, 0.75f);


    // 自定义场景的路径。
    public override string? CustomScenePath => "res://CuteSakikoMod/scenes/Ancient/another_self.tscn";

    // 自定义地图图标和轮廓的路径
    public override string? CustomMapIconPath => "res://CuteSakikoMod/images/ui/anotherself_icon.png";

    public override string? CustomMapIconOutlinePath => "res://CuteSakikoMod/images/ui/anotherself_icon_outline.png";

    // 历史记录图标路径
    public override string? CustomRunHistoryIconPath => "res://CuteSakikoMod/images/ui/anotherself_icon.png";

    public override string? CustomRunHistoryIconOutlinePath =>
        "res://CuteSakikoMod/images/ui/anotherself_icon_outline.png";

    // 生成选项。每个选项有自己的池子。
    protected override OptionPools MakeOptionPools { get; } = new(
        MakePool(
            AncientOption<Mask>(),
            AncientOption<Anchor>()
        ),
        
        MakePool(
            AncientOption<LizardTail>(),
            AncientOption<ArcaneScroll>()
        ),
        MakePool(
            AncientOption<YummyCookie>(), // 加权重，权重越高越容易取到
            AncientOption<WingCharm>()
        )
    );

    // 出现条件：第二幕或第三幕
    public override bool IsValidForAct(ActModel act)
    {
        var actNum = act.ActNumber();
        return actNum == 2 || actNum == 3;
    }
}