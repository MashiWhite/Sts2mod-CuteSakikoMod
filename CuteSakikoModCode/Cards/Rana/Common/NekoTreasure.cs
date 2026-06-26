using CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Basic;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class NekoTreasure : CuteRanaCard
{
    public NekoTreasure() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new CardsVar(2)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 抽2张牌
        await CardPileCmd.Draw(choiceContext, 2, Owner);

        // 确定添加的猫咪数量（升级前1，升级后2）
        int addCount = IsUpgraded ? 2 : 1;
        if (addCount <= 0) return;

        // 获取所有 Neko 卡模板
        var allNekoCards = ModelDb.CardPool<CuteSakikoTokenCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
            .ToList();
        if (allNekoCards.Count == 0) return;

        var combatState = Owner.Creature.CombatState!;
        var rng = Owner.RunState.Rng.CombatCardGeneration;

        for (int i = 0; i < addCount; i++)
        {
            var template = rng.NextItem(allNekoCards);
            var newCard = combatState.CreateCard(template, Owner);
            newCard.EnergyCost.SetThisCombat(0, true); // 本场战斗免费
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Draw, Owner,CardPilePosition.Random);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在 OnPlay 中通过 addCount 处理
    }
}