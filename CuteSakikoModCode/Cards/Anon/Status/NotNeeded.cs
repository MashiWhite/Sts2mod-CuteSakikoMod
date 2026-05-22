using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;

public class NotNeeded() : ModStatusCard(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override bool HasTurnEndInHandEffect => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Retain;
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new BlockVar(2m, ValueProp.Move); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Task.CompletedTask;
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        var blockAmount = DynamicVars.Block.IntValue;
        if (blockAmount > 0)
            await CreatureCmd.GainBlock(Owner.Creature, (decimal)blockAmount, ValueProp.Move, null, false);

        var copy = CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
    }

    // 有保留关键词 → 留在手牌；否则进入弃牌堆
    protected override PileType GetResultPileTypeForOnTurnEndInHandEffect() =>
        Keywords.Contains(CardKeyword.Retain) ? PileType.Hand : PileType.Discard;

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}