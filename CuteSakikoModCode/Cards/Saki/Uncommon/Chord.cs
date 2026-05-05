using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class Chord() : CuteSakikoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 定义琴奏牌的类型列表（用于随机生成）
    private static readonly Type[] QinPlayTypes = new[]
    {
        typeof(StrikeFast),
        typeof(StrikeSlow),
        typeof(StrikeOpulent)
    };

    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.Playpiano];

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var pressure = Owner.Creature.GetPower<PressurePower>();
            if (pressure == null) return false;
            return IsUpgraded || pressure.Amount >= 1;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromCard<StrikeSlow>();
            yield return HoverTipFactory.FromCard<StrikeOpulent>();
            yield return HoverTipFactory.FromCard<StrikeFast>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cardsToGenerate = 0;
        if (IsUpgraded)
        {
            cardsToGenerate = 2;
        }
        else
        {
            var pressure = Owner.Creature.GetPower<PressurePower>();
            if (pressure != null && pressure.Amount >= 1)
            {
                await PowerCmd.ModifyAmount(choiceContext, pressure, -1, Owner.Creature, this);
                cardsToGenerate = 2;
            }
            else
            {
                cardsToGenerate = 1;
            }
        }

        if (cardsToGenerate <= 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var availableTypes = QinPlayTypes.ToList();
        var selectedTypes = new List<Type>();
        for (var i = 0; i < cardsToGenerate && availableTypes.Count > 0; i++)
        {
            var chosen = rng.NextItem(availableTypes);
            selectedTypes.Add(chosen);
            availableTypes.Remove(chosen);
        }

        foreach (var type in selectedTypes)
        {
            CardModel tempCard;
            if (type == typeof(StrikeFast))
                tempCard = CombatState.CreateCard<StrikeFast>(Owner);
            else if (type == typeof(StrikeSlow))
                tempCard = CombatState.CreateCard<StrikeSlow>(Owner);
            else if (type == typeof(StrikeOpulent))
                tempCard = CombatState.CreateCard<StrikeOpulent>(Owner);
            else
                continue;

            if (IsUpgraded && tempCard.IsUpgradable)
                CardCmd.Upgrade(tempCard);

            if (tempCard.Pile?.Type != PileType.Hand)
                await CardPileCmd.Add(tempCard, PileType.Hand);

            tempCard.ExhaustOnNextPlay = false;
            await CardCmd.AutoPlay(choiceContext, tempCard, cardPlay.Target);

            if (tempCard.Pile != null && tempCard.Pile.IsCombatPile)
                await CardPileCmd.RemoveFromCombat(tempCard);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后无需消耗压力
    }
}