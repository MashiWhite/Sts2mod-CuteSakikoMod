using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class AllForget() : CuteSakikoModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile == null) return;
        var handCards = handPile.Cards.ToList();
        if (handCards.Count == 0) return;

        // 统计记忆牌数量（在遗忘前）
        var memoryCount = handCards.Count(card => card.Keywords.Contains(CutesakiKeywords.Memory.GetModCardKeyword()));

        // 遗忘所有手牌
        await MemoryCmd.Forget(choiceContext, handCards, this);

        // 若遗忘的记忆牌 ≥5 张，给所有敌人施加崩溃
        if (memoryCount >= 5)
        {
            var combatState = Owner.Creature.CombatState;
            if (combatState != null)
                foreach (var enemy in combatState.Enemies.Where(e => e.IsAlive))
                    await PowerCmd.Apply<BreakDownPower>(choiceContext, enemy, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}