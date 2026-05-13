using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MemoryComingPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }
    
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        // 使用规范模板列表，而不是可变实例
        var canonicalCards = MemoryCardPile.GetCanonicalCards(player);
        if (canonicalCards.Count == 0) return;

        var count = Amount;
        var randomCards = CardFactory.GetDistinctForCombat(
            Owner.Player,
            canonicalCards,
            count,
            Owner.Player.RunState.Rng.CombatCardGeneration
        ).ToList();

        if (randomCards.Count == 0) return;

        var mutableCards = randomCards.Select(card => card.IsMutable ? card : card.ToMutable()).ToList();
        await CardPileCmd.AddGeneratedCardsToCombat(mutableCards, PileType.Hand, player);
        Flash();
    }
}