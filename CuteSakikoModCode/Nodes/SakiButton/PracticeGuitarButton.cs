using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes.SakiButton;

public partial class PracticeGuitarButton : ModActionButton
{
    private AnonGuitar? _guitar;

    protected override void OnInit()
    {
        var state = RunManager.Instance.DebugOnlyGetState();
        var player = state != null ? LocalContext.GetMe(state.Players) : null;
        _guitar = player?.Relics?.OfType<AnonGuitar>().FirstOrDefault();
        if (_guitar == null) return;

        // 调用基类自适应布局
        ApplyCustomLayout(180f, 180f);

        var texture = GD.Load<Texture2D>("res://CuteSakikoMod/images/others/others/practice_guitar_icon.png");
        var img = new TextureRect();
        img.Texture = texture;
        img.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        img.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        img.MouseFilter = MouseFilterEnum.Ignore;
        img.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(img);

        Visible = true;
    }

    protected override bool CanTrigger => _guitar != null;

    protected override void ExecuteAction()
    {
        if (_guitar == null) return;
        var screen = new ChordManagementScreen();
        screen.SetChords(_guitar.GetOrCreateChords());
        screen.ShowScreen();
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        var title = new LocString("rest_site_ui", "CUTE_SAKIKO_MOD_OPTION_PRACTICE_GUITAR_NAME");
        var desc = new LocString("rest_site_ui", "CUTE_SAKIKO_MOD_PRACTICE_GUITAR_DESC");
        var tip = new HoverTip(title, desc);
        var tipSet = NHoverTipSet.CreateAndShow(this, tip);
        if (tipSet == null) return;
        var alignment = GlobalPosition.X > GetViewportRect().Size.X * 0.6f
            ? HoverTipAlignment.Left
            : HoverTipAlignment.Right;
        tipSet.SetAlignment(this, alignment);
        var viewportRect = GetViewportRect();
        var pos = tipSet.GlobalPosition;
        if (pos.X < 0) pos.X = 10;
        if (pos.X + tipSet.Size.X > viewportRect.Size.X) pos.X = viewportRect.Size.X - tipSet.Size.X - 10;
        if (pos.Y < 0) pos.Y = 10;
        if (pos.Y + tipSet.Size.Y > viewportRect.Size.Y) pos.Y = viewportRect.Size.Y - tipSet.Size.Y - 10;
        tipSet.GlobalPosition = pos;
    }

    protected override void OnUnfocus()
    {
        NHoverTipSet.Remove(this);
        base.OnUnfocus();
    }
}