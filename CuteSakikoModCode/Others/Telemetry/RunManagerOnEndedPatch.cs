using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;

/// <summary>
/// 拦截 RunManager.OnEnded，拿到返回的 SerializableRun，本地化后上传。
/// 只在单机或主机端上传，避免多人时每人都传一遍。
/// </summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded))]
public static class RunManagerOnEndedPatch
{
    [HarmonyPostfix]
    public static void Postfix(SerializableRun __result, bool isVictory)
    {
        if (__result == null) return;

        try
        {
            // 多人时只有主机/单机上传，避免重复
            var netType = RunManager.Instance?.NetService?.Type;
            if (netType == NetGameType.Client) return;

            // isVictory 单独记一下，因为 SerializableRun 里没有直接字段
            // （WinTime > 0 只是近似判断，这里直接传参更准确）
            CuteSakikoModTelemetry.CaptureLocalizedRun(__result);
        }
        catch (System.Exception ex)
        {
            Entry.Logger.Warn($"[Telemetry] OnEnded patch failed: {ex.Message}");
        }
    }
}