
using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.EventButton;

/// <summary>
/// 在远古事件结束时，抓取所有选项的选中/跳过状态并上传遥测。
/// UpdateRunHistory 在 Done() 内被调用，此时 GeneratedOptions 的 WasChosen 已确定。
/// </summary>
[HarmonyPatch(typeof(AncientEventModel), "UpdateRunHistory")]
public static class AncientEventModelUpdateRunHistoryPatch
{
    // 反射缓存：私有字段 _generatedOptions (List<EventOption>?)
    private static readonly FieldInfo? GeneratedOptionsField =
        typeof(AncientEventModel).GetField("_generatedOptions",
            BindingFlags.NonPublic | BindingFlags.Instance);

    [HarmonyPostfix]
    private static void Postfix(AncientEventModel __instance)
    {
        try
        {
            var player = __instance.Owner;
            if (player == null) return;

            var eventId = __instance.Id.Entry;
            var characterId = player.Character.Id.Entry;
            var floor = CuteSakikoModTelemetry.GetCurrentFloor(player);

            // 抓取所有选项
            var choices = new List<(string key, string title, bool chosen)>();

            if (GeneratedOptionsField?.GetValue(__instance) is System.Collections.IEnumerable rawOptions)
            {
                foreach (var obj in rawOptions)
                {
                    if (obj is not EventOption opt) continue;

                    string key = opt.TextKey ?? "";
                    string title = "";
                    try { title = opt.Title?.GetFormattedText() ?? ""; } catch { }

                    bool chosen = opt.WasChosen;
                    choices.Add((key, title, chosen));
                }
            }

            // 回退：如果拿不到 GeneratedOptions，就从 RunState 的历史记录读
            if (choices.Count == 0)
            {
                var entry = player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId);
                if (entry?.AncientChoices != null)
                {
                    foreach (var c in entry.AncientChoices)
                    {
                        string key = c.Title?.LocEntryKey ?? "";
                        string title = "";
                        try { title = c.Title?.GetFormattedText() ?? ""; } catch { }
                        choices.Add((key, title, c.WasChosen));
                    }
                }
            }

            if (choices.Count == 0) return;

            // 发送完整选项信息（包含被跳过的）
            CuteSakikoModTelemetry.CaptureEventChoices(eventId, characterId, floor, choices);

            // 额外为每个被跳过的选项单独发一条，方便按 option_key 聚合
            foreach (var (key, title, chosen) in choices)
            {
                if (!chosen)
                {
                    CuteSakikoModTelemetry.CaptureEventOptionSkipped(
                        eventId, characterId, floor, key, title);
                }
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[Telemetry] AncientEvent choice capture failed: {ex.Message}");
        }
    }
}