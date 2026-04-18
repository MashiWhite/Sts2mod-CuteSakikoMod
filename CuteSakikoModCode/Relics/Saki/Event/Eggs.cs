using BaseLib.Abstracts;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Event;

[Pool(typeof(CuteSakiRelicPool))]
public sealed class Eggs : CustomRelicModel
{
    [SavedProperty] private readonly List<ModelId> _gainedEggCards = new();

    [SavedProperty] private bool _hasGivenChoice; // 关键：保存是否已经给过彩蛋牌

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override string BigIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").BigRelicImagePath();

    public override string PackedIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").RelicImagePath();

    protected override string PackedIconOutlinePath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + "_outline.png").RelicImagePath();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Eggs); }
    }

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

        var prompt = new LocString("CUTESAKIKOMOD-EGGS.selectPrompt", "选择一张彩蛋牌");
        var prefs = new CardSelectorPrefs(prompt, 1);
        var choiceContext = new BlockingPlayerChoiceContext();
        var selectedCard = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            available,
            player,
            prefs)).FirstOrDefault();
        if (selectedCard == null) return;

        // 永久加入牌库
        var permanentCard = player.RunState.CreateCard(selectedCard, player);
        await CardPileCmd.Add(permanentCard, PileType.Deck);
        EggCardGainedEvent.Trigger(permanentCard);
        CardCmd.Preview(permanentCard);

        // 如果当前在战斗中，则生成一张临时复制加入手牌（用于本场战斗）
        if (player.Creature.CombatState != null)
        {
            var tempCard = player.Creature.CombatState.CreateCard(selectedCard, player);
            if (permanentCard.IsUpgraded && tempCard.IsUpgradable)
                CardCmd.Upgrade(tempCard);
            await CardPileCmd.AddGeneratedCardToCombat(tempCard, PileType.Hand, true);
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