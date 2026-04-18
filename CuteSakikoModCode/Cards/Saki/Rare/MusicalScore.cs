using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

[Pool(typeof(CuteSakiCardPool))]
public class MusicalScore() : CustomCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [CardKeyword.Retain] : [];

    // 悬停提示：显示回忆中的和弦（根据升级状态显示对应版本）
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (IsUpgraded)
            {
                // 创建升级版实例用于提示（仅用于显示，不实际加入游戏）
                var upgradedChord = ModelDb.Card<ChordMemory>().ToMutable();
                upgradedChord.UpgradeInternal();
                upgradedChord.FinalizeUpgradeInternal();
                yield return HoverTipFactory.FromCard(upgradedChord);
            }
            else
            {
                yield return HoverTipFactory.FromCard<ChordMemory>();
            }
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 收集所有牌堆中的状态牌
        var statusCards = new List<CardModel>();
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust })
        {
            var pile = pileType.GetPile(Owner);
            if (pile != null) statusCards.AddRange(pile.Cards.Where(c => c.Type == CardType.Status));
        }

        // 将所有状态牌转化为回忆中的和弦
        foreach (var statusCard in statusCards)
        {
            var targetCard = CombatState.CreateCard<ChordMemory>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(targetCard);
            await CardCmd.Transform(statusCard, targetCard);
        }

        // 额外抽2张牌
        await CardPileCmd.Draw(choiceContext, 1, Owner);

        // 施加能力，使本回合所有牌费用减一（包括后续抽到的）
        await PowerCmd.Apply<MusicalScorePower>(Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级仅添加保留关键词，已在 CanonicalKeywords 中处理
    }
}