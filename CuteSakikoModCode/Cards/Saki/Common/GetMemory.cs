using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class GetMemory() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(10m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var canonicalCards = MemoryCardPile.GetCanonicalCards(Owner);
        if (canonicalCards.Count == 0) return;

        // 打乱而不消耗 CombatCardGeneration
        var shuffled = canonicalCards.UnstableShuffle(Owner.RunState.Rng.Shuffle);
        var cardTemplate = shuffled.FirstOrDefault();
        if (cardTemplate == null) return;

        var randomCard = MemoryCardPile.CreateCardFromMemorySnapshot(Owner, cardTemplate);
        if (randomCard != null)
        {
            if (IsUpgraded && !randomCard.IsUpgraded)
            {
                randomCard.UpgradeInternal();
                randomCard.FinalizeUpgradeInternal();
            }
            await CardPileCmd.AddGeneratedCardToCombat(randomCard, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}