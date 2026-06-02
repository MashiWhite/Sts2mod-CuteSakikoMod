using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
// MemoryCmd

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class MemoryBurning : CuteSakikoModCard
{
    public MemoryBurning() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 收集手牌、抽牌堆、弃牌堆中的所有回忆牌（按顺序避免重复）
        var memoryCards = new List<CardModel>();
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust })
        {
            var pile = pileType.GetPile(Owner);
            if (pile != null)
                // 只添加尚未收集的牌（防止同一张牌在多个牌堆）
                memoryCards.AddRange(pile.Cards
                    .Where(c => c.Keywords.Contains(CutesakiKeywords.Memory.GetModCardKeyword()))
                    .Where(c => !memoryCards.Contains(c)));
        }

        if (memoryCards.Count == 0)
            return;

        // 获取一个随机敌人作为攻击牌的目标（若无则留空）
        var target = GetRandomEnemy();

        foreach (var card in memoryCards)
        {
            // 如果牌不在手牌中，先移入手牌（参照 Legato 的做法）
            if (card.Pile?.Type != PileType.Hand)
            {
                card.RemoveFromCurrentPile();
                await CardPileCmd.Add(card, PileType.Hand);
            }

            // 自动打出（若有可用目标）
            if (target != null)
                await CardCmd.AutoPlay(choiceContext, card, target);
            else
                // 如果没有敌人，但牌可能需要目标，则跳过打出直接遗忘
                // 可根据需求调整
                ;

            // 立即遗忘该牌
            await MemoryCmd.Forget(choiceContext, new List<CardModel> { card }, this);
        }
    }

    /// <summary> 获取随机可命中敌人（参考 ByMyReign）</summary>
    private Creature? GetRandomEnemy()
    {
        var enemies = CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return null;
        return enemies[Owner.RunState.Rng.CombatCardSelection.NextInt(enemies.Count)];
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}