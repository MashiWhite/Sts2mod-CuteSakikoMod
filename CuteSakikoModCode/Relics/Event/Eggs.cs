using CuteSakikoMod.CuteSakikoModCode.Cards;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.RunData;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public sealed class Eggs : CuteSakiRelic
{
    [SavedProperty] private readonly List<ModelId> _gainedEggCards = new();

    public static PlayerRunSavedData<PlayerEggsData>? PlayerEggsSlot { get; set; }

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.Eggs];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        EggCardGainedEvent.OnEggCardGained += OnEggCardGained;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;

        if (PlayerEggsSlot != null)
        {
            var data = PlayerEggsSlot.Get(player);
            if (data.HasSelected) return;
        }

        await Cmd.Wait(0.1f);
        await GiveEggCardChoice(choiceContext, player);
    }

    public override async Task AfterRemoved()
    {
        EggCardGainedEvent.OnEggCardGained -= OnEggCardGained;
        await base.AfterRemoved();
    }

    private void OnEggCardGained(CardModel card)
    {
        if (!_gainedEggCards.Contains(card.Id))
            _gainedEggCards.Add(card.Id);
    }

    private async Task GiveEggCardChoice(PlayerChoiceContext choiceContext, Player player)
    {
        // 获取所有继承自 CuteSakikoModEggCard 的卡牌
        var allEggCards = ModelDb.AllCards
            .Where(c => typeof(CuteSakikoModEggCard).IsAssignableFrom(c.GetType()))
            .ToList();

        if (allEggCards.Count == 0) return;

        var tempCards = allEggCards.Select(can => player.RunState.CreateCard(can, player)).ToList();
        var prompt = new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_EGGS.selectPrompt");
        var prefs = new CardSelectorPrefs(prompt, 1);

        var selectedCards = await CardSelectCmd.FromSimpleGrid(choiceContext, tempCards, player, prefs);
        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard == null) return;

        var canonical = ModelDb.GetById<CardModel>(selectedCard.Id);
        var permanentCard = player.RunState.CreateCard(canonical, player);
        await CardPileCmd.Add(permanentCard, PileType.Deck);
        EggCardGainedEvent.Trigger(permanentCard);
        CardCmd.Preview(permanentCard);

        if (player.Creature.CombatState != null)
        {
            var tempCard = player.Creature.CombatState.CreateCard(canonical, player);
            if (permanentCard.IsUpgraded && tempCard.IsUpgradable)
                CardCmd.Upgrade(tempCard);
            await CardPileCmd.AddGeneratedCardToCombat(tempCard, PileType.Hand, player);
        }

        PlayerEggsSlot?.Modify(player, data => data.HasSelected = true);
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> options,
        CardCreationOptions creationOptions)
    {
        if (Owner != player) return false;
        if (player.RunState.CurrentRoom is not CombatRoom combatRoom || combatRoom.RoomType != RoomType.Boss)
            return false;
        if (creationOptions.Source != CardCreationSource.Encounter) return false;

        // 获取所有继承自 CuteSakikoModEggCard 且尚未获得的卡牌
        var allEggCards = ModelDb.AllCards
            .Where(c => typeof(CuteSakikoModEggCard).IsAssignableFrom(c.GetType()))
            .ToList();

        var available = allEggCards.Where(c => !_gainedEggCards.Contains(c.Id)).ToList();
        if (available.Count == 0) return false;

        var selected = player.RunState.Rng.UpFront.NextItem(available);
        var cardResult = CardFactory.CreateForReward(
            player,
            1,
            new CardCreationOptions(new[] { selected }, CardCreationSource.Encounter, CardRarityOddsType.Uniform)
                .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications)
        ).FirstOrDefault();
        if (cardResult == null) return false;

        options.Add(cardResult);
        return true;
    }
}