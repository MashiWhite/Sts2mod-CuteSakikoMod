
using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class EmoDuetEvent : CuteSakikoEvent
{
    private IHoverTip[]? _enchantTips;
    private IHoverTip[]? _relicTips;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/emo_duet.png"
    );

    public override bool IsShared => false;

    protected override bool IsAllowedInternal(IRunState runState) => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            new(this, ListenClosely, InitialOptionKey("LISTEN_CLOSELY"))
        };
    }

    private Task ListenClosely()
    {
        _enchantTips ??= HoverTipFactory.FromEnchantment<Glam>().ToArray();
        _relicTips ??= HoverTipFactory.FromRelic<EmoMask>().ToArray();

        var options = new List<EventOption>
        {
            new(this, HearArgument, ModOptionKey("SECOND", "HEAR_ARGUMENT"), _enchantTips),
            new(this, SlipAway, ModOptionKey("SECOND", "SLIP_AWAY"))
        };

        if (HasSakiOrObAndAnon())
        {
            options.Add(new(this, AskWhatIsEmo, ModOptionKey("SECOND", "ASK_WHAT_IS_EMO"), _relicTips));
        }
        else
        {
            options.Add(new EventOption(this, null, ModOptionKey("SECOND", "ASK_WHAT_IS_EMO_LOCKED")));
        }

        SetEventState(PageDescription("SECOND"), options);
        return Task.CompletedTask;
    }

    private bool HasSakiOrObAndAnon()
    {
        if (Owner?.RunState == null) return false;
        var hasSakiOrOb = Owner.RunState.Players.Any(p => p.Character is CuteSaki || p.Character is CuteOb);
        var hasAnon = Owner.RunState.Players.Any(p => p.Character is CuteAnon);
        return hasSakiOrOb && hasAnon;
    }

    // 选项1：听完整场争论 → 附魔“华彩”（Glam）
    private async Task HearArgument()
    {
        var canonicalEnchantment = ModelDb.Enchantment<Glam>();
        var prefs = new CardSelectorPrefs(PageDescription("SELECT_CARD_TITLE"), 1, 1)
        {
            RequireManualConfirmation = false
        };

        bool Filter(CardModel card) => card.Enchantment == null;

        var selected = await CardSelectCmd.FromDeckForEnchantment(
            Owner!, canonicalEnchantment, 1, Filter, prefs);

        foreach (var card in selected)
        {
            var mutableEnchantment = canonicalEnchantment.ToMutable();
            CardCmd.Enchant(mutableEnchantment, card, 1);
        }

        SetEventFinished(PageDescription("HEAR_ARGUMENT_DESC"));
    }

    // 选项2：趁乱溜走 → 受伤 8 点，移除 1 张牌
    private async Task SlipAway()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            8m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null
        );

        var selected = await CardSelectCmd.FromDeckForRemoval(
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)
        );
        var card = selected.FirstOrDefault();
        if (card != null)
        {
            await CardPileCmd.RemoveFromDeck(card);
        }

        SetEventFinished(PageDescription("SLIP_AWAY_DESC"));
    }

    // 选项3：问“Emo 是什么” → 获得遗物
    private async Task AskWhatIsEmo()
    {
        await RelicCmd.Obtain(ModelDb.Relic<EmoMask>().ToMutable(), Owner!);
        SetEventFinished(PageDescription("ASK_WHAT_IS_EMO_DESC"));
    }

    private LocString PageDescription(string pageKey) => L10NLookup($"{Id.Entry}.pages.{pageKey}.description");
}