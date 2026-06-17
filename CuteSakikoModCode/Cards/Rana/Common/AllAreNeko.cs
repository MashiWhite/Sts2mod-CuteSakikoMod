using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class AllAreNeko : CuteRanaCard
{
    public AllAreNeko() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new CatsVar();
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获取所有猫咪卡牌模板
        var allNekoCards = ModelDb.CardPool<CuteSakikoTokenCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
            .ToList();
        if (allNekoCards.Count == 0) return;

        var combatState = Owner.Creature.CombatState!;
        var rng = Owner.RunState.Rng.CombatCardGeneration;

        // 计算场上存活生物总数（敌人 + 玩家生物）
        int aliveEnemies = combatState.Enemies.Count(e => e.IsAlive);
        int alivePlayers = combatState.Players.Select(p => p.Creature).Count(c => c.IsAlive);
        int totalCreatures = aliveEnemies + alivePlayers;

        var results = new List<CardPileAddResult>();

        for (int i = 0; i < totalCreatures; i++)
        {
            var template = rng.NextItem(allNekoCards);
            var newCard = combatState.CreateCard(template, Owner);
            // 添加的猫咪不升级
            var result = await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Draw, Owner);
            results.Add(result);
        }

        // 通知 UI 刷新抽牌堆数字
        if (results.Count > 0)
            CardCmd.PreviewCardPileAdd(results);
    }

    protected override void OnUpgrade()
    {
        // 移除消耗关键词
        RemoveKeyword(CardKeyword.Exhaust);
    }

    /// <summary>
    /// 动态变量：实时显示场上存活生物数
    /// </summary>
    private class CatsVar : DynamicVar
    {
        public CatsVar() : base("Cats", 0m)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            var combat = card.CombatState;
            if (combat != null)
            {
                int aliveEnemies = combat.Enemies.Count(e => e.IsAlive);
                int alivePlayers = combat.Players.Select(p => p.Creature).Count(c => c.IsAlive);
                BaseValue = aliveEnemies + alivePlayers;
            }
            else
            {
                BaseValue = 0;
            }
        }
    }
}