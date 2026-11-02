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
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class SteamEvent : CuteSakikoEvent
{
    private IHoverTip[]? _relicTips;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/steam.png"
    );

    public override bool IsShared => false;

    /// <summary>需要爱音+祥子，归“需要 mod 角色的事件”。</summary>
    protected override bool NeedsModCharacter => true;

    protected override bool IsAllowedInternal(IRunState runState)
    {
        return true;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            new(this, ListenClosely, InitialOptionKey("LISTEN_CLOSELY"))
        };
    }

    // 第一段 → 第二段
    private Task ListenClosely()
    {
        SetEventState(
            PageDescription("SECOND"),
            new List<EventOption>
            {
                new(this, WatchClosely, ModOptionKey("SECOND", "WATCH_CLOSELY"))
            });
        return Task.CompletedTask;
    }

    // 第二段 → 第三段（带三个选项）
    private Task WatchClosely()
    {
        _relicTips ??= HoverTipFactory.FromRelic<MaskInSteam>().ToArray();

        var options = new List<EventOption>
        {
            new(this, SitAtEdge, ModOptionKey("THIRD", "SIT_AT_EDGE")),
            new(this, TakePhoto, ModOptionKey("THIRD", "TAKE_PHOTO"))
        };

        if (HasAnonAndSaki())
            options.Add(new EventOption(this, GiveMaskToAnon, ModOptionKey("THIRD", "GIVE_MASK_TO_ANON"), _relicTips));
        else
            options.Add(new EventOption(this, null, ModOptionKey("THIRD", "GIVE_MASK_TO_ANON_LOCKED")));

        SetEventState(PageDescription("THIRD"), options);
        return Task.CompletedTask;
    }

    private bool HasAnonAndSaki()
    {
        if (Owner?.RunState == null) return false;
        var hasAnon = Owner.RunState.Players.Any(p => p.Character is CuteAnon);
        var hasSaki = Owner.RunState.Players.Any(p => p.Character is CuteSaki);
        return hasAnon && hasSaki;
    }

    // 选项1：在池边坐下泡脚 → 恢复 20% 最大生命值
    private async Task SitAtEdge()
    {
        var healAmount = Owner!.Creature.MaxHp * 0.2m;
        await CreatureCmd.Heal(Owner.Creature, healAmount);
        SetEventFinished(PageDescription("SIT_AT_EDGE_DESC"));
    }

    // 选项2：掏出相机 → 失去 8 点生命值，移除 1 张牌
    private async Task TakePhoto()
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
        if (card != null) await CardPileCmd.RemoveFromDeck(card);

        SetEventFinished(PageDescription("TAKE_PHOTO_DESC"));
    }

    // 选项3：假面给爱音 → 获得遗物，进入后续对话
    private async Task GiveMaskToAnon()
    {
        await RelicCmd.Obtain(ModelDb.Relic<MaskInSteam>().ToMutable(), Owner!);

        SetEventState(
            PageDescription("GIVE_MASK"),
            new List<EventOption>
            {
                new(this, ShowEpilogue, ModOptionKey("GIVE_MASK", "CONTINUE"))
            });
    }

    private Task ShowEpilogue()
    {
        SetEventFinished(PageDescription("EPILOGUE"));
        return Task.CompletedTask;
    }

    private LocString PageDescription(string pageKey)
    {
        return L10NLookup($"{Id.Entry}.pages.{pageKey}.description");
    }
}