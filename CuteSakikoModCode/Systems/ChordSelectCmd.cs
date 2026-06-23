using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class ChordSelectCmd
{
    public static async Task<List<string>> SelectChords(
        PlayerChoiceContext context,
        Player player,
        int count)
    {
        var runManager = RunManager.Instance;
        var sync = runManager.PlayerChoiceSynchronizer;
        var choiceId = sync.ReserveChoiceId(player);

        await context.SignalPlayerChoiceBegun(PlayerChoiceOptions.CancelPlayCardActions);

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
    /// 用于练习吉他等休息站选项：让玩家从给定的候选和弦中自由选择要保留的和弦。
    /// 无需 PlayerChoiceContext，内部使用 PlayerChoiceSynchronizer 进行多人同步。
    /// </summary>
    public static async Task<List<string>> SelectChordsForPractice(
        Player player,
        List<string> candidateIds,
        LocString prompt)  // 注意：这里是 LocString 类型
    {
        var sync = RunManager.Instance.PlayerChoiceSynchronizer;
        var choiceId = sync.ReserveChoiceId(player);

        if (LocalContext.IsMe(player))
        {
            var screen = new ChordLibraryScreen();
            var selected = await screen.ShowFreeSelection(candidateIds, prompt);

            var indexes = selected
                .Select(id => ChordManager.AllChordsList.FindIndex(c => c.Id == id))
                .ToList();
            sync.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndexes(indexes));
            return selected;
        }
        else
        {
            var remoteResult = await sync.WaitForRemoteChoice(player, choiceId);
            var indexes = remoteResult.AsIndexes();
            return indexes.Select(i => ChordManager.AllChordsList[i].Id).ToList();
        }
    }
}