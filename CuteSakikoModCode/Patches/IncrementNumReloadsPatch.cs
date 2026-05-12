using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedMultiPlayer))]
static class SetUpSavedMultiPlayerPatch
{
    static void Postfix(RunManager __instance, RunState state, LoadRunLobby lobby)
    {
        // 仅在多人主机端处理
        if (lobby.NetService.Type != NetGameType.Host)
            return;

        foreach (var player in state.Players)
        {
            var timeWatch = player.Relics.OfType<TimeWatch>().FirstOrDefault();
            if (timeWatch != null)
                timeWatch.ReloadCount++;
        }
    }
}