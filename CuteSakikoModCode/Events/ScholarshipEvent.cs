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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class ScholarshipEvent : CuteSakikoEvent
{
    private IHoverTip[]? _relicTips;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/scholarship.png"
    );

    public override bool IsShared => true;

    // 基础 60，CalculateVars 里 ±10，最终范围 50~70
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(60)
    ];

    public override void CalculateVars()
    {
        var gold = DynamicVars.Gold;
        gold.BaseValue += Rng.NextInt(-10, 11);
    }

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
        _relicTips ??= HoverTipFactory.FromRelic<GradeFirstNotebook>().ToArray();

        var options = new List<EventOption>
        {
            new(this, AskForSignature, ModOptionKey("SECOND", "ASK_FOR_SIGNATURE")),
            new(this, Schadenfreude, ModOptionKey("SECOND", "SCHADENFREUDE"))
        };

        if (HasAnonAndSakiOrOb())
        {
            options.Add(new(this, AskForTutoring, ModOptionKey("SECOND", "ASK_FOR_TUTORING"), _relicTips));
        }
        else
        {
            options.Add(new EventOption(this, null, ModOptionKey("SECOND", "ASK_FOR_TUTORING_LOCKED")));
        }

        SetEventState(PageDescription("SECOND"), options);
        return Task.CompletedTask;
    }

    private bool HasAnonAndSakiOrOb()
    {
        if (Owner?.RunState == null) return false;
        var hasAnon = Owner.RunState.Players.Any(p => p.Character is CuteAnon);
        var hasSakiOrOb = Owner.RunState.Players.Any(p => p.Character is CuteSaki || p.Character is CuteOb);
        return hasAnon && hasSakiOrOb;
    }

    // 选项1：举起手要签名 → 获得 50~70 金币（由 CalculateVars 随机决定）
    private async Task AskForSignature()
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner!);
        SetEventFinished(PageDescription("ASK_FOR_SIGNATURE_DESC"));
    }

    // 选项2：幸灾乐祸地吹口哨 → 失去 8 点生命，移除 1 张牌
    private async Task Schadenfreude()
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

        SetEventFinished(PageDescription("SCHADENFREUDE_DESC"));
    }

    // 选项3：请求补习 → 获得遗物，进入后续对话
    private async Task AskForTutoring()
    {
        await RelicCmd.Obtain(ModelDb.Relic<GradeFirstNotebook>().ToMutable(), Owner!);

        SetEventState(
            PageDescription("ASK_FOR_TUTORING"),
            new[] { new EventOption(this, ShowEpilogue, ModOptionKey("ASK_FOR_TUTORING", "CONTINUE")) }
        );
    }

    private Task ShowEpilogue()
    {
        SetEventFinished(PageDescription("EPILOGUE"));
        return Task.CompletedTask;
    }

    private LocString PageDescription(string pageKey) => L10NLookup($"{Id.Entry}.pages.{pageKey}.description");
}