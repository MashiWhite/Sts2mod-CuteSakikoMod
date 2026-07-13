using CuteSakikoMod.CuteSakikoModCode.Nodes;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;

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
    
}