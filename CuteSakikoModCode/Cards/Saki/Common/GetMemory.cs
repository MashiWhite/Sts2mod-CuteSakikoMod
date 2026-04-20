using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;


public class GetMemory() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        try
        {
            // 打出时获得3层压力
            await PowerCmd.Apply<PressurePower>(Owner.Creature, 3, Owner.Creature, this);

            // 获取本场战斗中已经被消耗的回忆卡牌的ModelId
            var exhaustedPile = PileType.Exhaust.GetPile(Owner);
            var exhaustedMemoryIds = exhaustedPile?.Cards
                .Where(card => card.CanonicalKeywords.Contains(CutesakiKeywords.Memory))
                .Select(card => card.Id)
                .ToHashSet() ?? new HashSet<ModelId>();

            // 从所有回忆卡牌中排除已消耗的
            var availableMemoryCards = ModelDb.AllCards
                .Where(card =>
                    card.CanonicalKeywords.Contains(CutesakiKeywords.Memory) && !exhaustedMemoryIds.Contains(card.Id))
                .ToList();

            if (availableMemoryCards.Count > 0)
            {
                var randomCard = CardFactory.GetDistinctForCombat(
                    Owner,
                    availableMemoryCards,
                    1,
                    Owner.RunState.Rng.CombatCardGeneration
                ).FirstOrDefault();

                if (randomCard != null)
                {
                    CardModel newCard;
                    if (randomCard.IsMutable)
                        newCard = randomCard;
                    else
                        newCard = randomCard.ToMutable();

                    if (IsUpgraded)
                    {
                        newCard.UpgradeInternal();
                        newCard.FinalizeUpgradeInternal();
                    }

                    await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, true);
                }
            }
        }
        finally
        {
            await CardCmd.Discard(choiceContext, this);
        }
    }

    protected override void OnUpgrade()
    {
    }
}