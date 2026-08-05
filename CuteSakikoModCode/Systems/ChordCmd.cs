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
    // ChordCmd.cs 中的 SelectChords 方法（完整替换）
    public static async Task<List<string>> SelectChords(
        PlayerChoiceContext context,
        Player player,
        int count,
        int multiplier = 0)  // 新增 multiplier 参数，默认为 0 表示基础值
    {
        var runManager = RunManager.Instance;
        var sync = runManager.PlayerChoiceSynchronizer;
        var choiceId = sync.ReserveChoiceId(player);

        await context.SignalPlayerChoiceBegun(player, PlayerChoiceOptions.CancelPlayCardActions);

        List<int> chordIndexes = null;

        if (runManager.NetService.NetId == player.NetId)
        {
            var screen = new ChordLibraryScreen();
            var selectedIds = await screen.ShowSelection(count, multiplier);
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

    public static bool AddRandomBonusChord(AnonGuitar guitar)
    {
        if (guitar?.Owner == null) return false;

        var owned = new HashSet<string>(guitar.GetAllEquippedChords());
        foreach (var id in guitar.GetLearnedChords())
            owned.Add(id);

        var allPools = new List<string>();
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

        var available = allPools.Where(id => !owned.Contains(id)).ToList();
        if (available.Count == 0) return false;

        var newChord = guitar.Owner.RunState.Rng.UpFront.NextItem(available);
        guitar.AddBonusChord(newChord);
        guitar.LearnChord(newChord);
        return true;
    }

    public static int AddRandomTemporaryChords(AnonGuitar guitar, int targetCount)
    {
        if (guitar?.Owner == null) return 0;

        var existing = guitar.GetTemporaryChords().ToList();
        int currentCount = existing.Count;
        if (currentCount >= targetCount) return 0;

        int needed = targetCount - currentCount;
        int added = 0;
        var rng = guitar.Owner.RunState.Rng.UpFront;

        // 1. 获取所有可学习和弦（全量池）
        var allPools = new List<string>();
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

        // 构建“已拥有”集合（已学、已装备、当前临时），用于过滤未学
        var owned = new HashSet<string>(guitar.GetAllEquippedChords());
        foreach (var id in guitar.GetLearnedChords())
            owned.Add(id);
        foreach (var id in existing)
            owned.Add(id);

        // 从未学且未拥有的池中选取（优先）
        var available = allPools.Where(id => !owned.Contains(id)).ToList();

        // 2. 优先从 available 中取（未学）
        while (added < needed && available.Count > 0)
        {
            var chordId = rng.NextItem(available);
            guitar.AddTemporaryChord(chordId);
            guitar.LearnChord(chordId);      // 永久学习
            available.Remove(chordId);       // 防止重复
            // 注意：该和弦现在已加入 owned，但我们不更新 owned 集合，因为 fallback 会从已学集合取，不影响
            added++;
        }

        // 3. 如果还需要，从已学习和弦中取（所有和弦都已学完，或 available 不够）
        if (added < needed)
        {
            var learned = guitar.GetLearnedChords().ToList(); // 所有已学
            if (learned.Count == 0) return added; // 如果没有已学的，无法继续

            while (added < needed)
            {
                var chordId = rng.NextItem(learned); // 从已学池随机取，允许重复
                guitar.AddTemporaryChord(chordId);
                // 不调用 LearnChord，因为已经学过了
                added++;
            }
        }

        return added;
    }

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