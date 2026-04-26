using Godot;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using CuteSakikoMod.CuteSakikoModCode.Systems;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes
{
    public partial class ChordButton : TextureButton
    {
        private string _chordId;
        private int _multiplier = 1;

        public void Setup(string chordId, int mult = 1)
        {
            _chordId = chordId;
            _multiplier = mult;

            var tex = ChordDisplayHelper.GetChordTexture(chordId);
            if (tex != null)
            {
                TextureNormal = tex;
            }
     
            StretchMode = StretchModeEnum.KeepAspectCentered;
            CustomMinimumSize = new Vector2(64, 64);

            MouseEntered += () =>
            {
                var tips = new List<IHoverTip>
                {
                    ChordDisplayHelper.GetChordHoverTip(_chordId, _multiplier)
                };
                var tipSet = NHoverTipSet.CreateAndShow(this, tips);
                if (tipSet != null)
                {
                    // 将提示框移动到按钮右侧（延迟一帧保证面板已创建）
                    CallDeferred(nameof(RepositionTooltip), tipSet);
                }
            };
            MouseExited += () => NHoverTipSet.Remove(this);
        }

        private void RepositionTooltip(NHoverTipSet tipSet)
        {
            if (tipSet is Control tipControl)
            {
                tipControl.GlobalPosition = GlobalPosition + new Vector2(Size.X + 10, 0);
            }
        }
    }
}