using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using CuteSakikoMod.CuteSakikoModCode.Singletons;

namespace CuteSakikoMod.CuteSakikoModCode.Patches
{
    [HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.LoadRunSave))]
    public static class ReloadCountSyncPatch
    {
        static void Postfix(ReadSaveResult<SerializableRun> __result)
        {
            if (__result.Success)
            {
                FlybackManager.SyncReloadCountIfHost();
            }
        }
    }
}