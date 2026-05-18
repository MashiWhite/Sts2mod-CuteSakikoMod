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
    // 整局游戏内查重（持久化，用于BOSS奖励）
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
        // 仅处理自己
        if (player != Owner) return;

        // 若本局已选择过，不再弹出
        if (PlayerEggsSlot != null)
        {
            var data = PlayerEggsSlot.Get(player);
            if (data.HasSelected) return;
        }

        await Cmd.Wait(0.1f);
        await GiveEggCardChoice();
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

    private async Task GiveEggCardChoice()
    {
        var player = Owner;
        if (player == null) return;

        var allEggCards = ModelDb.CardPool<CuteSakikoEggCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .ToList();

        // ★ 首次选择展示所有彩蛋卡，不使用 _gainedEggCards 过滤
        var available = allEggCards;
        if (available.Count == 0) return;

        var tempCards = available.Select(can => player.RunState.CreateCard(can, player)).ToList();
        var prompt = new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_EGGS.selectPrompt"); // 修复键名
        var prefs = new CardSelectorPrefs(prompt, 1);
        var choiceContext = new BlockingPlayerChoiceContext();
        var selectedCard =
            (await CardSelectCmd.FromSimpleGrid(choiceContext, tempCards, player, prefs)).FirstOrDefault();
        if (selectedCard == null) return;

        var canonical = ModelDb.GetById<CardModel>(selectedCard.Id);
        var permanentCard = player.RunState.CreateCard(canonical, player);
        await CardPileCmd.Add(permanentCard, PileType.Deck);
        EggCardGainedEvent.Trigger(permanentCard); // 加入 _gainedEggCards 用于后续BOSS奖励
        CardCmd.Preview(permanentCard);

        if (player.Creature.CombatState != null)
        {
            var tempCard = player.Creature.CombatState.CreateCard(canonical, player);
            if (permanentCard.IsUpgraded && tempCard.IsUpgradable)
                CardCmd.Upgrade(tempCard);
            await CardPileCmd.AddGeneratedCardToCombat(tempCard, PileType.Hand, player);
        }

        // 标记本局已选择过（持久化）
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

        var allEggCards = ModelDb.CardPool<CuteSakikoEggCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .ToList();
        // BOSS奖励使用持久化列表查重
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