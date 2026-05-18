using CuteSakikoMod.CuteSakikoModCode.Singletons;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.LoadRunSave))]
public static class ReloadCountSyncPatch
{
    private static void Postfix(ReadSaveResult<SerializableRun> __result)
    {
        if (__result.Success) FlybackManager.SyncReloadCountIfHost();
    }
}