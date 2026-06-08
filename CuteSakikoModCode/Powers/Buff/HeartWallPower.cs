using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class HeartWallPower : CuteSakikoModPower
{
    private readonly HashSet<ModelId> _immuneCardIds = new();
    private readonly HashSet<ModelId> _damagingCardsThisPlay = new();
    

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // 动态变量：用于在描述中显示已免疫的卡牌列表
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new StringVar("ImmuneCards"); }
    }

    private void UpdateImmuneCardsDisplay()
    {
        var cardTitles = string.Join("\n", _immuneCardIds.Select(id =>
        {
            var card = ModelDb.GetById<CardModel>(id);
            return card != null ? "- " + card.Title : "- " + id.Entry;
        }));
        // 若列表为空，显示“无”
        if (string.IsNullOrEmpty(cardTitles))
            cardTitles = "- 无";
        ((StringVar)DynamicVars["ImmuneCards"]).StringValue = cardTitles;
    }
    
    // 伤害结算时检查是否免疫
    public override Decimal ModifyHpLostAfterOsty(
        Creature target, Decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || amount <= 0) return amount;
        if (cardSource != null && _immuneCardIds.Contains(cardSource.Id))
            return 0;
        return amount;
    }

    // 只要受到来自卡牌的未格挡伤害，就记录该卡牌为本轮造成过伤害
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage == 0) return;
        if (cardSource != null)
            _damagingCardsThisPlay.Add(cardSource.Id);
    }

    // 卡牌完全结算后，将造成过伤害的卡牌 ID 加入免疫列表
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card != null && _damagingCardsThisPlay.Contains(card.Id))
        {
            _immuneCardIds.Add(card.Id);
            UpdateImmuneCardsDisplay();
        }
        _damagingCardsThisPlay.Remove(card.Id); // 清理当前卡牌标记
    }

    // 回合开始时清空记录
    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            _immuneCardIds.Clear();
            _damagingCardsThisPlay.Clear();
            UpdateImmuneCardsDisplay();
        }
    }
}