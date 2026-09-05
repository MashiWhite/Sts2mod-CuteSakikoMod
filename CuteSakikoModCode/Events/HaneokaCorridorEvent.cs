
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;
using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class HaneokaCorridorEvent : CuteSakikoEvent
{
    private IHoverTip[]? _relicHoverTips;
    
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/haneoka_corridor.png"
    );

    public override bool IsShared => true;

    protected override bool IsAllowedInternal(IRunState runState) => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>
        {
            new(this, Listen, InitialOptionKey("LISTEN"))
        };

        if (HasSakiOrOb())
        {
            options.Add(new(this, OpenDoorPiano, InitialOptionKey("OPEN_DOOR_PIANO")));
        }
        else
        {
            options.Add(new EventOption(this, null, InitialOptionKey("OPEN_DOOR_PIANO_LOCKED")));
        }

        if (HasSakiOrObAndAnon())
        {
            _relicHoverTips ??= HoverTipFactory.FromRelic<FlaxenHairedGirl>().ToArray();
            options.Add(new(this, OpenDoorAnon, InitialOptionKey("OPEN_DOOR_ANON"), _relicHoverTips));
        }
        else
        {
            options.Add(new EventOption(this, null, InitialOptionKey("OPEN_DOOR_ANON_LOCKED")));
        }

        return options;
    }

    private bool HasSakiOrOb()
    {
        if (Owner?.RunState == null) return false;
        return Owner.RunState.Players.Any(p => p.Character is CuteSaki || p.Character is CuteOb);
    }

    private bool HasSakiOrObAndAnon()
    {
        if (!HasSakiOrOb()) return false;
        return Owner!.RunState.Players.Any(p => p.Character is CuteAnon);
    }

    // 选项1：靠近门口听 - 获得 1 次无色卡牌奖励
    private async Task Listen()
    {
        var cardReward = new CardReward(
            CardCreationOptions.ForNonCombatWithDefaultOdds(new[] { ModelDb.CardPool<ColorlessCardPool>() })
                .WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications),
            3,
            Owner
        );
        await RewardsCmd.OfferCustom(Owner, new List<Reward> { cardReward });
        SetEventFinished(PageDescription("LISTEN_SUCCESS"));
    }

    // 选项2：推开门 - 将所有初始打击变形为随机琴奏
    private async Task OpenDoorPiano()
    {
        var deck = PileType.Deck.GetPile(Owner!);
        var strikes = deck.Cards
            .Where(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Strike))
            .ToList();

        if (strikes.Count == 0)
        {
            SetEventFinished(PageDescription("PIANO_NO_STRIKES"));
            return;
        }

        // 收集所有琴奏卡牌（排除 PianoStrike）
        var playPianoKeyword = CutesakiKeywords.Playpiano.GetModCardKeyword();
        var pianoCardIds = ModelDb.AllCards
            .Where(c => c.CanonicalKeywords.Contains(playPianoKeyword) &&
                        c.Id != ModelDb.Card<PianoStrike>().Id)
            .Select(c => c.Id)
            .ToList();

        if (pianoCardIds.Count == 0)
        {
            SetEventFinished(PageDescription("PIANO_NO_CARDS"));
            return;
        }

        var transformations = new List<CardTransformation>();
        foreach (var strike in strikes)
        {
            var targetId = pianoCardIds[Owner.PlayerRng.Transformations.NextInt(pianoCardIds.Count)];
            var newCard = Owner.RunState.CreateCard(ModelDb.GetById<CardModel>(targetId), Owner);
            // 保留升级
            if (strike.IsUpgraded && newCard.IsUpgradable)
            {
                CardCmd.Upgrade(newCard);
            }
            // 保留附魔（如果目标可附魔）
            if (strike.Enchantment != null)
            {
                var ench = (EnchantmentModel)strike.Enchantment.MutableClone();
                if (ench.CanEnchant(newCard))
                {
                    CardCmd.Enchant(ench, newCard, ench.Amount);
                }
            }
            transformations.Add(new CardTransformation(strike, newCard));
        }

        await CardCmd.Transform(transformations, Owner.PlayerRng.Transformations);
        SetEventFinished(PageDescription("PIANO_SUCCESS"));
    }

    // 选项3：推开门 - 获得遗物“亚麻色头发的少女”
    private async Task OpenDoorAnon()
    {
    // 授予遗物
    var relicFinal = ModelDb.Relic<FlaxenHairedGirl>().ToMutable();
    await RelicCmd.Obtain(relicFinal, Owner!);

    // 显示第一段剧情，并提供“继续”按钮进入第二段
    SetEventState(
        PageDescription("ANON_SUCCESS_PART1"),
        new List<EventOption>
        {
            new(this, ShowSecondPart, ModOptionKey("ANON_SUCCESS", "CONTINUE"))
        }
    );
}

private Task ShowSecondPart()
{
    SetEventFinished(PageDescription("ANON_SUCCESS_PART2"));
    return Task.CompletedTask;
}

    private LocString PageDescription(string pageKey) => L10NLookup($"{Id.Entry}.pages.{pageKey}.description");
}