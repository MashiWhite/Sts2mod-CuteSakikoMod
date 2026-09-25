using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Enchantments;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class MysteriousMallEvent : CuteSakikoEvent
{
    private IHoverTip[]? _relicTips;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/mysterious_mall.png"
    );

    public override bool IsShared => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(4)
    ];

    protected override bool IsAllowedInternal(IRunState runState) => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            new(this, EnterMall, InitialOptionKey("ENTER_MALL"))
        };
    }

    private Task EnterMall()
    {
        var options = new List<EventOption>();

        if (HasRanaPlayer())
        {
            options.Add(new(this, CheckPillar, ModOptionKey("INSIDE", "CHECK_PILLAR")));
        }
        else
        {
            options.Add(new EventOption(this, null, ModOptionKey("INSIDE", "CHECK_PILLAR_LOCKED")));
        }

        options.Add(new(this, FindExit, ModOptionKey("INSIDE", "FIND_EXIT")));

        SetEventState(PageDescription("INSIDE"), options);
        return Task.CompletedTask;
    }

    private bool HasRanaPlayer()
    {
        if (Owner?.RunState == null) return false;
        return Owner.RunState.Players.Any(p => p.Character is CuteRana);
    }

    // 选项1.1：柱子旁好像有什么东西（需要乐奈）
    private Task CheckPillar()
    {
        _relicTips ??= HoverTipFactory.FromRelic<BagOfMatchaCandy>().ToArray();

        SetEventState(
            PageDescription("PILLAR"),
            new List<EventOption>
            {
                new(this, TakeVent, ModOptionKey("PILLAR", "TAKE_VENT"), _relicTips)
            });
        return Task.CompletedTask;
    }

    // 选项1.1.1：从通风口钻出来 → 获得遗物
    private async Task TakeVent()
    {
        await RelicCmd.Obtain(ModelDb.Relic<BagOfMatchaCandy>().ToMutable(), Owner!);
        SetEventFinished(PageDescription("VENT_DESC"));
    }

    // 选项1.2：继续寻找出口
    private Task FindExit()
    {
        var enchantTips = HoverTipFactory.FromEnchantment<MyGoEnchantment>().ToArray();

        SetEventState(
            PageDescription("FIND_EXIT"),
            new List<EventOption>
            {
                new(this, CheckSelf, ModOptionKey("FIND_EXIT", "CHECK_SELF"), enchantTips)
            });
        return Task.CompletedTask;
    }

    // 选项1.2.1：检查自己 → 随机为 {Cards} 张牌附魔 MyGo了
    private async Task CheckSelf()
    {
        var enchantment = ModelDb.Enchantment<MyGoEnchantment>();
        int count = DynamicVars.Cards.IntValue;

        var deck = PileType.Deck.GetPile(Owner!);
        var candidates = deck.Cards.Where(c => c.Enchantment == null).ToList();

        var chosen = new List<CardModel>();
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            var idx = Rng.NextInt(candidates.Count);
            chosen.Add(candidates[idx]);
            candidates.RemoveAt(idx);
        }

        foreach (var card in chosen)
        {
            CardCmd.Enchant(enchantment.ToMutable(), card, 1);
        }

        SetEventFinished(PageDescription("CHECK_SELF_DESC"));
    }

    private LocString PageDescription(string pageKey) => L10NLookup($"{Id.Entry}.pages.{pageKey}.description");
}