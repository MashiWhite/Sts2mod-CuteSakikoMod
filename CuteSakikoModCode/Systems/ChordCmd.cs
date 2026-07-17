using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class ChordCmd
{
    public static async Task<List<string>> SelectChords(
        PlayerChoiceContext context,
        Player player,
        int count)
    {
        var runManager = RunManager.Instance;
        var sync = runManager.PlayerChoiceSynchronizer;
        var choiceId = sync.ReserveChoiceId(player);

        await context.SignalPlayerChoiceBegun(player,PlayerChoiceOptions.CancelPlayCardActions);

        List<int> chordIndexes = null;

        if (runManager.NetService.NetId == player.NetId)
        {
            var screen = new ChordLibraryScreen();
            var selectedIds = await screen.ShowSelection(count);
            if (selectedIds != null && selectedIds.Count == count)
                chordIndexes = selectedIds
                    .Select(id => ChordManager.AllChordsList.FindIndex(c => c.Id == id))
                    .ToList();
            else
                chordIndexes = new List<int>();

            sync.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndexes(chordIndexes));
        }
        else
        {
            var remoteResult = await sync.WaitForRemoteChoice(player, choiceId);
            chordIndexes = remoteResult.AsIndexes();
        }

        await context.SignalPlayerChoiceEnded();

        if (chordIndexes == null || chordIndexes.Count == 0)
            return new List<string>();

        return chordIndexes.Select(i => ChordManager.AllChordsList[i].Id).ToList();
    }

    /// <summary>
    /// 为指定吉他添加一个随机的可学习 Bonus 和弦，并自动加入已学习列表。
    /// 所有客户端调用结果一致（全局同步随机数 UpFront）。
    /// </summary>
    public static bool AddRandomBonusChord(AnonGuitar guitar)
    {
        if (guitar?.Owner == null) return false;

        var ownedChordIds = new HashSet<string>();
        foreach (var kv in guitar.GetCurrentChords())
            if (!string.IsNullOrEmpty(kv.Value))
                ownedChordIds.Add(kv.Value);
        foreach (var id in guitar.GetBonusChords())
            ownedChordIds.Add(id);

        var allPools = new List<string>();
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

        var available = allPools.Where(id => !ownedChordIds.Contains(id)).ToList();
        if (available.Count == 0) return false;

        var newChord = guitar.Owner.RunState.Rng.UpFront.NextItem(available);
        guitar.AddBonusChord(newChord);
        guitar.LearnChord(newChord); // 可选
        return true;
    }

    /// <summary>
    /// 为指定吉他添加指定数量的临时和弦（随机、不重复），同时加入已学习列表。
    /// 所有客户端调用结果一致（全局同步随机数 UpFront）。
    /// </summary>
    public static int AddRandomTemporaryChords(AnonGuitar guitar, int targetCount)
    {
        if (guitar?.Owner == null) return 0;

        var existing = guitar.GetTemporaryChords().ToList();
        int currentCount = existing.Count;
        if (currentCount >= targetCount) return 0;

        var needed = targetCount - currentCount;

        var ownedChordIds = new HashSet<string>();
        foreach (var kv in guitar.GetCurrentChords())
            if (!string.IsNullOrEmpty(kv.Value))
                ownedChordIds.Add(kv.Value);
        foreach (var id in guitar.GetBonusChords())
            ownedChordIds.Add(id);
        foreach (var id in existing)
            ownedChordIds.Add(id);

        var allPools = new List<string>();
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

        var available = allPools.Where(id => !ownedChordIds.Contains(id)).ToList();
        if (available.Count == 0) return 0;

        var rng = guitar.Owner.RunState.Rng.UpFront;
        int added = 0;
        for (int i = 0; i < needed; i++)
        {
            if (available.Count == 0) break;
            var chordId = rng.NextItem(available);
            guitar.AddTemporaryChord(chordId);
            guitar.LearnChord(chordId);
            available.Remove(chordId);
            added++;
        }

        return added;
    }

    /// <summary>
    /// 随机学习指定数量的新和弦（不自动装备），使用战斗内随机数 CombatCardGeneration。
    /// </summary>
    public static List<string> LearnRandomChords(AnonGuitar guitar, int count)
    {
        if (guitar?.Owner == null) return new List<string>();

        var pool = new List<string>();
        foreach (ChordCategory cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            pool.AddRange(ChordManager.GetLearnableChordIds(cat));

        var alreadyKnown = new HashSet<string>(guitar.GetLearnedChords());
        var available = pool.Where(id => !alreadyKnown.Contains(id) && !ChordManager.AllChords[id].IsTemporaryOnly).ToList();

        if (available.Count == 0) return new List<string>();

        var rng = guitar.Owner.RunState.Rng.CombatCardGeneration;
        var toLearn = available.OrderBy(_ => rng.NextFloat()).Take(count).ToList();

        foreach (var chordId in toLearn)
            guitar.LearnChord(chordId);

        return toLearn;
    }

    /// <summary>
    /// 偶然弹奏一个随机和弦并储存，不学习。
    /// </summary>
    public static async Task AddRandomImprovisedChord(AnonGuitar guitar, PlayerChoiceContext context)
    {
        if (guitar?.Owner == null) return;

        var pool = new List<string>();
        pool.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
        pool.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
        pool.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));
        if (pool.Count == 0) return;

        var randomChordId = guitar.Owner.RunState.Rng.CombatCardSelection.NextItem(pool);
        await guitar.AddChordToStored(context, randomChordId);
    }
}