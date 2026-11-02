using System.Collections.Generic;
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
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class MysteriousMallEvent : CuteSakikoEvent
{
    private IHoverTip[]? _relicTips;
    private IHoverTip[]? _enchantTips;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/mysterious_mall.png"
    );

    public override bool IsShared => true;

    /// <summary>需要乐奈才能解锁专属选项，归“需要 mod 角色的事件”。</summary>
    protected override bool NeedsModCharacter => true;



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

    // ---------- 进入商场：三选项，提示已在这里挂好 ----------
    private Task EnterMall()
    {
        _relicTips ??= HoverTipFactory.FromRelic<BagOfMatchaCandy>().ToArray();
        _enchantTips ??= HoverTipFactory.FromEnchantment<MyGoEnchantment>().ToArray();

        var options = new List<EventOption>();

        if (HasRanaPlayer())
            options.Add(new EventOption(this, CheckPillar, ModOptionKey("INSIDE", "CHECK_PILLAR"), _relicTips));
        else
            options.Add(new EventOption(this, null, ModOptionKey("INSIDE", "CHECK_PILLAR_LOCKED")));

        options.Add(new EventOption(this, FindExit, ModOptionKey("INSIDE", "FIND_EXIT"), _enchantTips));
        options.Add(new EventOption(this, SitDown, ModOptionKey("INSIDE", "SIT_DOWN")));

        SetEventState(PageDescription("INSIDE"), options);
        return Task.CompletedTask;
    }

    private bool HasRanaPlayer()
    {
        if (Owner?.RunState == null) return false;
        return Owner.RunState.Players.Any(p => p.Character is CuteRana);
    }

    // ---------- 选项1：柱子旁 ----------
    // 提示已挂在 INSIDE 的选项上，这里不再传
    private Task CheckPillar()
    {
        SetEventState(PageDescription("PILLAR"),
            new List<EventOption>
            {
                new(this, TakeVent, ModOptionKey("PILLAR", "TAKE_VENT"))
            });
        return Task.CompletedTask;
    }

    private async Task TakeVent()
    {
        await RelicCmd.Obtain(ModelDb.Relic<BagOfMatchaCandy>().ToMutable(), Owner!);
        SetEventFinished(PageDescription("VENT_DESC"));
    }

    // ---------- 选项2：寻找出口 ----------
    private Task FindExit()
    {
        SetEventState(PageDescription("FIND_EXIT"),
            new List<EventOption>
            {
                new(this, CheckSelf, ModOptionKey("FIND_EXIT", "CHECK_SELF"))
            });
        return Task.CompletedTask;
    }

    // 选项2.1：检查自己 → 随机为 4 张牌附魔 MyGo了（附魔前先预览）
    private async Task CheckSelf()
    {
        var enchantment = ModelDb.Enchantment<MyGoEnchantment>();
        var count = DynamicVars.Cards.IntValue;

        var deck = PileType.Deck.GetPile(Owner!);

        // ★ 排除诅咒牌 + 已有附魔的牌
        var candidates = deck.Cards
            .Where(c => c.Enchantment == null && c.Type != CardType.Curse)
            .ToList();

        // 随机挑选
        var chosen = new List<CardModel>();
        for (var i = 0; i < count && candidates.Count > 0; i++)
        {
            var idx = Rng.NextInt(candidates.Count);
            chosen.Add(candidates[idx]);
            candidates.RemoveAt(idx);
        }

        // 附魔前预览
        if (chosen.Count > 0)
        {
            var previews = chosen
                .Select(c => new CardPileAddResult
                {
                    cardAdded = c,
                    success = true,
                    oldPile = deck,
                    targetPile = PileType.Deck,
                })
                .ToList();

            CardCmd.PreviewCardPileAdd(previews, 0.5f);
            await Cmd.Wait(0.5f);
        }

        foreach (var card in chosen)
            CardCmd.Enchant(enchantment.ToMutable(), card, 1);

        SetEventFinished(PageDescription("CHECK_SELF_DESC"));
    }

    // ---------- 选项3：坐下休息 ----------
    private Task SitDown()
    {
        SetEventState(PageDescription("SIT_DOWN"),
            new List<EventOption>
            {
                new(this, Sleep, ModOptionKey("SIT_DOWN", "SLEEP"))
            });
        return Task.CompletedTask;
    }

    private Task Sleep()
    {
        RandomizeNextFourMapPoints(Owner!.RunState);
        SetEventFinished(PageDescription("SLEEP_DESC"));
        return Task.CompletedTask;
    }

    /// <summary>
    /// 将当前坐标往下 4 行的所有地图节点类型随机化。
    /// </summary>
    private static void RandomizeNextFourMapPoints(IRunState runState)
    {
        var map = runState.Map;
        if (map == null) return;

        var currentCoord = runState.CurrentMapCoord;
        if (currentCoord == null) return;

        var startRow = currentCoord.Value.row + 1;
        var endRow = System.Math.Min(map.GetRowCount() - 1, startRow + 3);
        if (endRow < startRow) return;

        var rng = runState.Rng.UpFront;
        var types = new[]
        {
            MapPointType.Monster,
            MapPointType.Elite,
            MapPointType.Unknown,
            MapPointType.Shop,
            MapPointType.Treasure,
            MapPointType.RestSite,
        };

        for (var row = startRow; row <= endRow; row++)
        {
            foreach (var point in map.GetPointsInRow(row))
            {
                if (point.PointType == MapPointType.Boss) continue;
                if (point.PointType == MapPointType.Ancient) continue;
                if (point.PointType == MapPointType.Unassigned) continue;

                point.PointType = types[rng.NextInt(types.Length)];
            }
        }
    }

    private LocString PageDescription(string pageKey) => L10NLookup($"{Id.Entry}.pages.{pageKey}.description");
}