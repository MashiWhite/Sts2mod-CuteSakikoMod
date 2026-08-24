using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class MasqueradeInvitationEvent : CuteSakikoEvent
{
    private IHoverTip[]? _relicHoverTips;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/masquerade_invitation.png"
    );

    public override bool IsShared => false;

    protected override bool IsAllowedInternal(IRunState runState) => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            new(this, EnterDarkness, InitialOptionKey("CONTINUE"))
        };
    }

    private Task EnterDarkness()
    {
        var options = new List<EventOption>
        {
            new(this, Accept, ModOptionKey("SECOND", "ACCEPT")),
            new(this, Refuse, ModOptionKey("SECOND", "REFUSE"))
        };

        if (HasOblivionisPlayer())
        {
            _relicHoverTips ??= HoverTipFactory.FromRelic<MasqueradeRhapsody>().ToArray();
            options.Add(new(this, BlindPerformance, ModOptionKey("SECOND", "BLIND_PERFORMANCE"), _relicHoverTips));
        }
        else
        {
            // 锁定选项：无回调，仅显示锁定文本
            options.Add(new EventOption(this, null, ModOptionKey("SECOND", "BLIND_PERFORMANCE_LOCKED")));
        }

        SetEventState(PageDescription("SECOND"), options);
        return Task.CompletedTask;
    }

    private bool HasOblivionisPlayer()
    {
        if (Owner?.RunState == null) return false;
        return Owner.RunState.Players.Any(p => p.Character is CuteOb || p.Character is CuteSaki);
    }

    private async Task Accept()
    {
        var healAmount = Owner!.Creature.MaxHp * 0.15m;
        await CreatureCmd.Heal(Owner.Creature, healAmount);
        SetEventFinished(PageDescription("ACCEPT_SUCCESS"));
    }

    private async Task Refuse()
    {
        var lossAmount = Owner!.Creature.MaxHp * 0.05m;
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            lossAmount,
            false
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

        SetEventFinished(PageDescription("REFUSE_SUCCESS"));
    }

    private async Task BlindPerformance()
    {
        var relic = ModelDb.Relic<MasqueradeRhapsody>().ToMutable();
        await RelicCmd.Obtain(relic, Owner!);
        SetEventFinished(PageDescription("BLIND_PERFORMANCE_SUCCESS"));
    }
}