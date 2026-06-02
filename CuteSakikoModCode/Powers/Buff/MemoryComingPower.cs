using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MemoryComingPower : CuteSakikoModPower
{
    private List<CardModel>? _allMemoryCards; // 缓存所有回忆卡牌
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    // 钩子，在玩家回合开始时
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var canonicalCards = MemoryCardPile.GetCanonicalCards(player);
        if (canonicalCards.Count == 0) return;

        var count = Math.Min(Amount, canonicalCards.Count);
        var shuffled = canonicalCards.UnstableShuffle(Owner.Player.RunState.Rng.Shuffle);
        var selectedTemplates = shuffled.Take(count).ToList();
        if (selectedTemplates.Count == 0) return;

        var mutableCards = selectedTemplates
            .Select(template => Owner.Player.Creature.CombatState.CreateCard(template, Owner.Player)).ToList();
        await CardPileCmd.AddGeneratedCardsToCombat(mutableCards, PileType.Hand, player);
        Flash();
    }
}