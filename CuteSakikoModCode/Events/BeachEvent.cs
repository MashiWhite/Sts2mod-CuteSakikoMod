using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class BeachEvent : CuteSakikoEvent
{
    private IHoverTip[]? _relicTips;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/beach.png"
    );

    public override bool IsShared => false;

    /// <summary>需要爱音+祥子才能解锁第三个选项，归“需要 mod 角色的事件”。</summary>
    protected override bool NeedsModCharacter => true;

    /// <summary>锁定选项需两名玩家（各持对应 mod 角色）才能解锁，属多人刚需，受「多人事件」三态设置控制。</summary>
    protected override bool RequiresMultiplayerToUnlock => true;

    protected override bool IsAllowedInternal(IRunState runState) => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            new(this, GoPage2, InitialOptionKey("CONTINUE"))
        };
    }

    private Task GoPage2()
    {
        SetEventState(PageDescription("SECOND"), new List<EventOption>
        {
            new(this, GoPage3, ModOptionKey("SECOND", "CONTINUE"))
        });
        return Task.CompletedTask;
    }

    private Task GoPage3()
    {
        _relicTips ??= HoverTipFactory.FromRelic<AnonSandyWetSocks>()
            .Concat(HoverTipFactory.FromRelic<SakiSandyWetSocks>())
            .ToArray();

        var options = new List<EventOption>
        {
            new(this, RunIntoSea, ModOptionKey("THIRD", "RUN_INTO_SEA")),
            new(this, TakeNap, ModOptionKey("THIRD", "TAKE_NAP"))
        };

        if (HasAnonAndSaki())
        {
            options.Add(new(this, TakeOffSocks, ModOptionKey("THIRD", "TAKE_OFF_SOCKS"), _relicTips));
        }
        else
        {
            options.Add(new EventOption(this, null, ModOptionKey("THIRD", "TAKE_OFF_SOCKS_LOCKED")));
        }

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

    // 选项1：光脚冲进海里帮忙 → 恢复 20% 最大生命值
    private async Task RunIntoSea()
    {
        var healAmount = Owner!.Creature.MaxHp * 0.2m;
        await CreatureCmd.Heal(Owner.Creature, healAmount);
        SetEventFinished(PageDescription("RUN_INTO_SEA_DESC"));
    }

    // 选项2：找棵椰子树睡午觉 → 失去 8 点生命值，移除 1 张牌
    private async Task TakeNap()
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

        SetEventFinished(PageDescription("TAKE_NAP_DESC"));
    }

    // 选项3：三段式 → 获得两个遗物
    private Task TakeOffSocks()
    {
        SetEventState(PageDescription("SOCKS_PART1"), new List<EventOption>
        {
            new(this, SocksPart2, ModOptionKey("SOCKS_PART1", "CONTINUE"))
        });
        return Task.CompletedTask;
    }

    private Task SocksPart2()
    {
        SetEventState(PageDescription("SOCKS_PART2"), new List<EventOption>
        {
            new(this, SocksPart3, ModOptionKey("SOCKS_PART2", "CONTINUE"))
        });
        return Task.CompletedTask;
    }

    private async Task SocksPart3()
    {
        await RewardsCmd.OfferCustom(Owner!, new List<Reward>
        {
            new RelicReward(ModelDb.Relic<AnonSandyWetSocks>().ToMutable(), Owner!),
            new RelicReward(ModelDb.Relic<SakiSandyWetSocks>().ToMutable(), Owner!)
        });

        SetEventFinished(PageDescription("SOCKS_PART3"));
    }

    private LocString PageDescription(string pageKey) => L10NLookup($"{Id.Entry}.pages.{pageKey}.description");
}