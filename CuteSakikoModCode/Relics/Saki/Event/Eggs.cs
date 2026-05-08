using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
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

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Event;

public sealed class Eggs : CuteSakiRelic
{
    [SavedProperty] private readonly List<ModelId> _gainedEggCards = new();

    [SavedProperty] private bool _hasGivenChoice; // 关键：保存是否已经给过彩蛋牌

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.Eggs];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        EggCardGainedEvent.OnEggCardGained += OnEggCardGained;
        // 等待第一次玩家回合开始
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (_hasGivenChoice) return;
        if (player != Owner) return;
        _hasGivenChoice = true;
        // 延迟一帧确保 UI 完全加载
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
        var available = allEggCards.Where(c => !_gainedEggCards.Contains(c.Id)).ToList();
        if (available.Count == 0) return;

        // ✅ 修复：为每个可选卡牌创建带 Owner 的临时副本，确保选择界面可以正确获取 player
        var tempCards = available.Select(can => player.RunState.CreateCard(can, player)).ToList();

        var prompt = new LocString("relics", "CUTESAKIKOMOD-EGGS.selectPrompt");
        var prefs = new CardSelectorPrefs(prompt, 1);
        var choiceContext = new BlockingPlayerChoiceContext();
        var selectedCard = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            tempCards, // 现在每个卡牌都有 Owner
            player,
            prefs)).FirstOrDefault();
        if (selectedCard == null) return;

        // 后续逻辑保持不变：根据选中卡牌的 ID 创建永久卡牌
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