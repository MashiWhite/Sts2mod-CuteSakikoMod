using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
#if STS2_AT_LEAST_0_113_0
using MegaCrit.Sts2.Core.Entities.Save;
#endif

namespace CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;

/// <summary>
///     拦截 RunManager.OnEnded，拿到本局运行的 SerializableRun，本地化后上传。
///     只在单机或主机端上传，避免多人时每人都传一遍。
///     ⚠️ 0.113 起 OnEnded 的返回类型从 SerializableRun 改为
///        PostRunProgressUpdateResult（record struct，本局运行在 .run 字段）。
/// </summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded))]
public static class RunManagerOnEndedPatch
{
#if STS2_AT_LEAST_0_113_0
    [HarmonyPostfix]
    public static void Postfix(PostRunProgressUpdateResult __result, bool isVictory)
    {
        Handle(__result.run, isVictory);
    }
#else
    [HarmonyPostfix]
    public static void Postfix(SerializableRun __result, bool isVictory)
    {
        Handle(__result, isVictory);
    }
#endif

    private static void Handle(SerializableRun? run, bool isVictory)
    {
        if (run == null) return;

        try
        {
            // 多人时只有主机/单机上传，避免重复
            var netType = RunManager.Instance?.NetService?.Type;
            if (netType == NetGameType.Client) return;

            // isVictory 直接透传给遥测：SerializableRun 里没有胜利标记，
            // 用 run.WinTime > 0 近似会漏掉"超时失败/中途退出"等情况。
            CuteSakikoModTelemetry.CaptureLocalizedRun(run, isVictory);
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[Telemetry] OnEnded patch failed: {ex.Message}");
        }
    }
}
