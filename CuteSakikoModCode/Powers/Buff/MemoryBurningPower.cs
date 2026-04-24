
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MemoryBurningPower : CuteSakikoModPower
{

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single; // 不可叠加
    public override bool AllowNegative => false;

    // 修改回忆卡牌的费用为 0
    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        // 只对当前玩家的卡牌生效
        if (card.Owner?.Creature != Owner) return false;

        // 只对回忆卡牌生效
        if (!card.CanonicalKeywords.Contains(CutesakiKeywords.Memory)) return false;

        // 将费用改为 0
        modifiedCost = 0;
        return true;
    }

    // 打出回忆卡牌后将其消耗
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card.Owner?.Creature != Owner) return;

        if (card.CanonicalKeywords.Contains(CutesakiKeywords.Memory)) await CardCmd.Exhaust(choiceContext, card);
    }
}