
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using CuteSakikoMod.CuteSakikoModCode.Others;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class LiveSweetPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single; // 不可叠加
    public override bool AllowNegative => false;

    // 获得能力时立刻触发：抽 3 张牌，获得 3 点能量
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this) return;
        if (amount <= 0) return;

        var player = Owner!.Player;
        await PlayerCmd.GainEnergy(1, player);
        await CardPileCmd.Draw(choiceContext, 3, player);
    }

    // 全局减费：所有带 RanaLive 关键词的卡牌费用 -1
    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner) return false;
        if (!card.Keywords.Contains(CutesakiKeywords.RanaLive.GetModCardKeyword())) return false;
        modifiedCost -= 1;
        return true;
    }

    // 回合结束时移除自身（费用自动恢复）
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }
}