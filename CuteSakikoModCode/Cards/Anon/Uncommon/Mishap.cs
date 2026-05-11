using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class Mishap() : CuteAnonCard(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 基础格挡 30，升级后 35
            yield return new BlockVar(30m, ValueProp.Move);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        // 丢弃所有手牌（复制列表后逐个丢弃，避免集合修改）
        var hand = PileType.Hand.GetPile(Owner);
        var cardsToDiscard = hand?.Cards.ToList() ?? new List<CardModel>();
        foreach (var card in cardsToDiscard)
            // 跳过此牌本身？通常打出时此牌已在Play区，不在手牌中，但仍需排除
            if (card != this)
                await CardCmd.Discard(choiceContext, card);

        // 获得格挡
        var blockAmount = DynamicVars.Block.IntValue;
        await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m); // 30 → 35
    }
}