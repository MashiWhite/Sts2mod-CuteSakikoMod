using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Linq;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Status;

public class Shirk() : ModStatusCard(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];

    // 允许升级
    public override int MaxUpgradeLevel => 1;

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;

        await Cmd.Wait(0.25f);

        var otherCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != this)
            .ToList();

        if (otherCards.Count == 0) return;

        if (IsUpgraded)
        {
            // 升级后：手动选择至少1张手牌丢弃（最多全部其他手牌）
            var prefs = new CardSelectorPrefs(
                CardSelectorPrefs.DiscardSelectionPrompt,
                1,
                otherCards.Count
            );

            var selected = await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                prefs,
                c => c != this,
                this
            );

            foreach (var toDiscard in selected.ToList())
                await CardCmd.Discard(choiceContext, toDiscard);
        }
        else
        {
            // 未升级：随机丢弃1张
            var randomCard = Owner.RunState.Rng.CombatCardSelection.NextItem(otherCards);
            await CardCmd.Discard(choiceContext, randomCard);
        }
    }

    protected override void OnUpgrade()
    {
    }
}