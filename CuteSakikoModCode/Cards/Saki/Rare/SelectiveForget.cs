using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class SelectiveForget() : CuteSakikoModCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(10m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 收集四个牌堆的所有卡牌
        var pileTypes = new[] { PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust };
        var allCards = new List<CardModel>();
        foreach (var pileType in pileTypes)
        {
            var pile = pileType.GetPile(Owner);
            if (pile != null)
                allCards.AddRange(pile.Cards);
        }

        if (allCards.Count == 0) return;

        // 按ID去重，每种卡牌只显示一次
        var uniqueCards = allCards.GroupBy(c => c.Id).Select(g => g.First()).ToList();

        // 必须选一张，不可取消
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);

        var chosenCards = await CardSelectCmd.FromSimpleGrid(choiceContext, uniqueCards, Owner, prefs);
        var chosen = chosenCards.FirstOrDefault();
        if (chosen == null) return;

        // 遗忘所有同ID的卡牌
        var cardsToForget = allCards.Where(c => c.Id == chosen.Id).ToList();
        if (cardsToForget.Count > 0)
            await MemoryCmd.Forget(choiceContext, cardsToForget, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}