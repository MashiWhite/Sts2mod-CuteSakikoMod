using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class ChordButton : TextureButton
{
    public string ChordId { get; private set; }
    private int _multiplier = 1;

    public void Setup(string chordId, int mult = 1)
    {
        ChordId = chordId;
        _multiplier = mult;

        var tex = ChordDisplayHelper.GetChordTexture(chordId);
        if (tex != null) TextureNormal = tex;

        StretchMode = StretchModeEnum.KeepAspectCentered;
        CustomMinimumSize = new Vector2(64, 64);

        MouseEntered += () =>
        {
            var tips = new List<IHoverTip>
            {
                ChordDisplayHelper.GetChordHoverTip(ChordId, _multiplier)
            };
            var tipSet = NHoverTipSet.CreateAndShow(this, tips);
            if (tipSet != null)
                CallDeferred(nameof(RepositionTooltip), tipSet);
        };
        MouseExited += () => NHoverTipSet.Remove(this);
    }

    private void RepositionTooltip(NHoverTipSet tipSet)
    {
        if (tipSet is Control tipControl)
            tipControl.GlobalPosition = GlobalPosition + new Vector2(Size.X + 10, 0);
    }
}