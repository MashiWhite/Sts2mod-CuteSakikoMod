
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

        // 若本局已选择过，不再弹出
        if (PlayerEggsSlot != null)
        {
            var data = PlayerEggsSlot.Get(player);
            if (data.HasSelected) return;
        }

        await Cmd.Wait(0.1f);
        await GiveEggCardChoice(choiceContext, player); // 传入上下文
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
        var allEggCards = ModelDb.CardPool<CuteSakikoEggCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .ToList();

        if (allEggCards.Count == 0) return;

        // 首次选择展示所有彩蛋卡，不使用 _gainedEggCards 过滤
        var tempCards = allEggCards.Select(can => player.RunState.CreateCard(can, player)).ToList();
        var prompt = new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_EGGS.selectPrompt");
        var prefs = new CardSelectorPrefs(prompt, 1);

        // 关键修改：使用传入的 choiceContext（非阻塞），而不是 new BlockingPlayerChoiceContext
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

        // 标记本局已选择过
        PlayerEggsSlot?.Modify(player, data => data.HasSelected = true);
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> options,
        CardCreationOptions creationOptions)
    {
        // ... 保持不变 ...
        if (Owner != player) return false;
        if (player.RunState.CurrentRoom is not CombatRoom combatRoom || combatRoom.RoomType != RoomType.Boss)
            return false;
        if (creationOptions.Source != CardCreationSource.Encounter) return false;

        var allEggCards = ModelDb.CardPool<CuteSakikoEggCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
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