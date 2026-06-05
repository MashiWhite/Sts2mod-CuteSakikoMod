using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Patches;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class RestSiteOptionsManager : SingletonModel
{
    public override bool ShouldReceiveCombatHooks => false;

    public void BindToSynchronizer()
    {
        var sync = RunManager.Instance.RestSiteSynchronizer;
        if (sync == null) return;

        sync.AfterPlayerOptionChosen -= HandleAfterPlayerOptionChosen;
        sync.AfterPlayerOptionChosen += HandleAfterPlayerOptionChosen;
    }

    private void HandleAfterPlayerOptionChosen(RestSiteOption option, bool success, ulong playerId)
    {
        if (!success || playerId != LocalContext.NetId) return;
        
        RitsuLibFramework.Logger.Info($"Option chosen: {option.OptionId}, success={success}");
        
        if (option is PracticeGuitarOption) return;

        var me = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());
        var myGuitar = me?.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (myGuitar == null) return;

        myGuitar.NormalOptionUsed = true;

        if (me.Relics.OfType<MiniatureTent>().Any())
        {
            RitsuLibFramework.Logger.Info("Has tent, skipping disable");
            return;
        }

        var allLocalOptions = RunManager.Instance.RestSiteSynchronizer.GetLocalOptions();
        foreach (var opt in allLocalOptions)
        {
            if (!(opt is PracticeGuitarOption))
            {
                RestSiteOptionPatch.RestSiteOption_IsEnabled_Patch.SetEnabled(opt, false);
            }
        }
    }
}

// 注意：此补丁必须放在同一个程序集中，且被 Harmony 扫描到
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Rooms.RestSiteRoom))]
public static class RestSiteRoomEnterPatch
{
    [HarmonyPatch("EnterInternal")] // 或 "Enter"，根据实际方法名
    [HarmonyPostfix]
    public static void OnEnterRestSiteRoom(MegaCrit.Sts2.Core.Rooms.AbstractRoom __instance, MegaCrit.Sts2.Core.Runs.IRunState? runState, bool isRestoringRoomStackBase)
    {
        if (runState == null) return;
        RitsuLibFramework.Logger.Info("Entered RestSiteRoom, resetting flags and clearing overrides");
        foreach (var player in runState.Players)
        {
            var guitar = player.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar != null)
            {
                guitar.NormalOptionUsed = false;
                guitar.PracticeUsedThisVisit = false;
                // 清除之前手动设置的 IsEnabled 覆盖
                var options = RunManager.Instance.RestSiteSynchronizer?.GetOptionsForPlayer(player);
                if (options != null)
                {
                    foreach (var opt in options)
                    {
                        if (!(opt is PracticeGuitarOption))
                            RestSiteOptionPatch.RestSiteOption_IsEnabled_Patch.SetEnabled(opt, true);
                    }
                }
            }
        }
    }
}