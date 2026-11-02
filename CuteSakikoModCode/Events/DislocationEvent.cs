using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class DislocationEvent : CuteSakikoEvent
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/dislocation.png"
    );

    public override bool IsShared => true;

    /// <summary>需要爱音+祥子才能触发（事件核心就是这两人的互换），归“需要 mod 角色的事件”。</summary>
    protected override bool NeedsModCharacter => true;

    protected override bool IsAllowedInternal(IRunState runState)
    {
        var hasAnon = runState.Players.Any(p => p.Character is CuteAnon);
        var hasSaki = runState.Players.Any(p => p.Character is CuteSaki);
        return hasAnon && hasSaki;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption> { new(this, GoPage2, InitialOptionKey("CONTINUE")) };
    }

    private Task GoPage2()
    {
        SetEventState(PageDescription("SECOND"),
            new List<EventOption> { new(this, GoPage3, ModOptionKey("SECOND", "CONTINUE")) });
        return Task.CompletedTask;
    }

    private Task GoPage3()
    {
        SetEventState(PageDescription("THIRD"),
            new List<EventOption> { new(this, GoPage4, ModOptionKey("THIRD", "CONTINUE")) });
        return Task.CompletedTask;
    }

    // 第四页直接带三个选项
    private Task GoPage4()
    {
        SetEventState(PageDescription("FOURTH"), new List<EventOption>
        {
            new(this, OptionWalkAway, ModOptionKey("FOURTH", "WALK_AWAY")),
            new(this, OptionSwapRelics, ModOptionKey("FOURTH", "SWAP_RELICS")),
            new(this, OptionCrashAgain, ModOptionKey("FOURTH", "CRASH_AGAIN"))
        });
        return Task.CompletedTask;
    }

    // ============ 选项1：治疗至最高血量 + 名字互换 ============
    private async Task OptionWalkAway()
    {
        // 1) 所有人回复到最高血量
        var maxHp = Owner!.RunState.Players.Max(p => p.Creature.CurrentHp);
        foreach (var p in Owner.RunState.Players)
        {
            if (p.Creature.IsDead) continue;
            var delta = maxHp - p.Creature.CurrentHp;
            if (delta > 0)
                await CreatureCmd.Heal(p.Creature, delta);
        }

        // 2) 爱音 ↔ 祥子 互换显示名
        var anon = Owner.RunState.Players.FirstOrDefault(p => p.Character is CuteAnon);
        var saki = Owner.RunState.Players.FirstOrDefault(p => p.Character is CuteSaki);
        if (anon != null && saki != null)
        {
            var platform = RunManager.Instance.NetService.Platform;
            var anonName = PlatformUtil.GetPlayerNameRaw(platform, anon.NetId);
            var sakiName = PlatformUtil.GetPlayerNameRaw(platform, saki.NetId);

            NameChangeCmd.SetPlayerNameDirect(anon, sakiName);
            NameChangeCmd.SetPlayerNameDirect(saki, anonName);
        }

        SetEventFinished(PageDescription("WALK_AWAY_DESC"));
    }

    // ============ 交换映射：爱音 ↔ 祥子优先，其余环形 ============
    private static Dictionary<Player, Player> BuildSwapMap(List<Player> players)
    {
        var map = new Dictionary<Player, Player>();

        var anons = players
            .Where(p => p.Character is CuteAnon)
            .OrderBy(p => p.NetId)
            .ToList();

        var sakis = players
            .Where(p => p.Character is CuteSaki)
            .OrderBy(p => p.NetId)
            .ToList();

        var pairCount = Math.Min(anons.Count, sakis.Count);
        var paired = new HashSet<Player>();
        for (var i = 0; i < pairCount; i++)
        {
            map[anons[i]] = sakis[i];
            map[sakis[i]] = anons[i];
            paired.Add(anons[i]);
            paired.Add(sakis[i]);
        }

        // 剩余玩家按原顺序环形 A→B→C→A
        var rest = players.Where(p => !paired.Contains(p)).ToList();
        if (rest.Count > 1)
            for (var i = 0; i < rest.Count; i++)
                map[rest[i]] = rest[(i + 1) % rest.Count];

        return map;
    }

    private async Task OptionSwapRelics()
{
    if (!LocalContext.IsMe(Owner)) return;

    var players = Owner!.RunState.Players.ToList();
    var netService = RunManager.Instance.NetService;

    RelicSwapCoordinator.ResetForNewExchange();

    // ---------- 1. 弹窗选遗物 ----------
    var myCandidates = Owner.Relics.ToList();
    var myPicked = new List<RelicModel>();
    int maxPick = Math.Min(2, myCandidates.Count);
    for (int i = 0; i < maxPick; i++)
    {
        var screen = NChooseARelicSelection.ShowScreen(myCandidates);
        var selected = (await screen.RelicsSelected()).FirstOrDefault();
        if (selected == null) break;
        myPicked.Add(selected);
        myCandidates.Remove(selected);
    }

    var myRelicIds = myPicked.Select(r => r.Id.Entry).ToList();

    // ---------- 2. 广播本机选择 ----------
    RelicSwapCoordinator.BroadcastChoice(Owner.NetId, myRelicIds);

    // ---------- 3. 收集所有玩家选择 ----------
    var allChoices = await RelicSwapCoordinator.CollectAllAsync(
        Owner.NetId, myRelicIds, players.Count);

    // ---------- 4. 计算 moves ----------
    var swapMap = BuildSwapMap(players);
    var moves = new List<(ulong OwnerNetId, string RelicId, ulong TargetNetId)>();

    foreach (var p in players)
    {
        if (!swapMap.TryGetValue(p, out var target)) continue;
        if (!allChoices.TryGetValue(p.NetId, out var relicIds)) continue;

        foreach (var relicId in relicIds)
            moves.Add((p.NetId, relicId, target.NetId));
    }

    if (moves.Count == 0)
    {
        SetEventFinished(PageDescription("SWAP_RELICS_NO_RELICS"));
        return;
    }

    // ---------- 5. host / 单机：执行 + 广播 ----------
    var isHostOrSingle = netService?.Type is NetGameType.Singleplayer or NetGameType.Host;
    if (isHostOrSingle)
    {
        var moveStrings = moves.Select(m => $"{m.OwnerNetId}|{m.RelicId}|{m.TargetNetId}").ToList();
        await RelicSwapCoordinator.ExecuteAndBroadcast(moveStrings);
    }

    SetEventFinished(PageDescription("SWAP_RELICS_DESC"));
}

    // ============ 选项3：从爱音+祥子卡池抽 5 次（两段文本） ============
    private async Task OptionCrashAgain()
    {
        var pools = new List<CardPoolModel>();
        foreach (var p in Owner!.RunState.Players)
            if (p.Character is CuteAnon || p.Character is CuteSaki)
                if (!pools.Contains(p.Character.CardPool))
                    pools.Add(p.Character.CardPool);

        var rewards = new List<Reward>();
        AddRewards(rewards, pools, CardRarity.Common, 2);
        AddRewards(rewards, pools, CardRarity.Uncommon, 2);
        AddRewards(rewards, pools, CardRarity.Rare, 1);

        await RewardsCmd.OfferCustom(Owner, rewards);

        // 第一段 → 继续按钮
        SetEventState(
            PageDescription("CRASH_AGAIN_PART1"),
            new List<EventOption>
            {
                new(this, ShowCrashAgainPart2, ModOptionKey("CRASH_AGAIN_PART1", "CONTINUE"))
            });
    }

    private Task ShowCrashAgainPart2()
    {
        SetEventFinished(PageDescription("CRASH_AGAIN_PART2"));
        return Task.CompletedTask;
    }

    private void AddRewards(List<Reward> rewards, List<CardPoolModel> pools, CardRarity rarity, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var opts = CardCreationOptions
                .ForNonCombatWithUniformOdds(pools, c => c.Rarity == rarity)
                .WithFlags(CardCreationFlags.NoRarityModification);
            rewards.Add(new CardReward(opts, 3, Owner!));
        }
    }

    private LocString PageDescription(string pageKey)
    {
        return L10NLookup($"{Id.Entry}.pages.{pageKey}.description");
    }
}