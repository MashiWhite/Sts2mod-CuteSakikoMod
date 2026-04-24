
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MusicaCaelestisPower : CuteSakikoModPower
{

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠层

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BreakDownPower>(); }
    }

    // 每层能力在回合开始时抽1张牌并获得1点能量
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        if (Amount <= 0) return;
        
        var draw = Amount;
        var energy = Amount;
        await CardPileCmd.Draw(choiceContext, draw, player);
        await PlayerCmd.GainEnergy(energy, player);
    }
    

    // 监听任何生物获得崩溃 (BreakDownPower)
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        // 如果变化的是 BreakDownPower 且是增加层数
        if (power is BreakDownPower && amount > 0)
            // 自身增加1层能力
            await PowerCmd.ModifyAmount(this, 1, null, null);
    }
}