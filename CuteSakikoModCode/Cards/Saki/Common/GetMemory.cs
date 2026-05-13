using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class GetMemory() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先给自己加压力（必须在 try 之外，确保即使随机获取失败也能执行）
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, 3, Owner.Creature, this);

        // 从记忆牌堆获取规范模板
        var canonicalCards = MemoryCardPile.GetCanonicalCards(Owner);
        if (canonicalCards.Count == 0) return;

        var randomCard = CardFactory.GetDistinctForCombat(
            Owner,
            canonicalCards,          // 传入规范模板
            1,
            Owner.RunState.Rng.CombatCardGeneration
        ).FirstOrDefault();

        if (randomCard != null)
        {
            if (IsUpgraded)
            {
                randomCard.UpgradeInternal();
                randomCard.FinalizeUpgradeInternal();
            }

            await CardPileCmd.AddGeneratedCardToCombat(randomCard, PileType.Hand, Owner);
        }

        // 最后将自己弃掉
        await CardCmd.Discard(choiceContext, this);
    }

    protected override void OnUpgrade()
    {
    }
}