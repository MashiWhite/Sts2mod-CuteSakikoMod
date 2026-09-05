using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

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

        // 获取遗忘堆
        var forgetPile = ForgetCardPile.Get(Owner);
        if (forgetPile == null || forgetPile.Cards.Count == 0) return;

        // 收集遗忘堆中的所有回忆牌
        var memoryCards = forgetPile.Cards
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Memory.GetModCardKeyword()))
            .ToList();

        if (memoryCards.Count == 0) return;

        // 获取随机敌人作为需要目标的牌的目标
        var target = GetRandomEnemy();

        foreach (var card in memoryCards)
        {
            // 从遗忘堆移除
            if (card.Pile == forgetPile)
                forgetPile.RemoveInternal(card);
            else
                card.RemoveFromCurrentPile();

            // 加入手牌
            await CardPileCmd.Add(card, PileType.Hand);

            // 自动打出（如果需要目标但无敌人则跳过打出，直接消耗）
            if (target != null)
                await CardCmd.AutoPlay(choiceContext, card, target);
            else
                // 没有目标时，如果卡牌不需要目标，则尝试打出；如果需要目标则放弃打出
                if (card.TargetType == TargetType.None || card.TargetType == TargetType.Self)
                    await CardCmd.AutoPlay(choiceContext, card, null);

            // 消耗该回忆牌
            await CardCmd.Exhaust(choiceContext, card);
        }

        // 通知遗忘堆内容变化
        forgetPile.InvokeContentsChanged();
    }

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