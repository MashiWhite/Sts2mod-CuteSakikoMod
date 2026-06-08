using System.Text.Json.Nodes;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;

namespace CuteSakikoMod.CuteSakikoModCode.Others
{
    public static class CuteSakikoModTelemetry
    {
        private const string ApplicantId = "CuteSakikoMod";
        private static ITelemetryClient Client = null!;

        
        public static void Register()
        {
            TelemetryRegistry.RegisterApplicant(new TelemetryApplicant
            {
                ApplicantId = ApplicantId,
                OwnerModId = "CuteSakikoMod",
                DisplayName = "CuteSakikoMod",
                DisplayNameText = ModSettingsText.Literal("CuteSakikoMod"),
                // 修改这里：使用 PostHogTelemetryAdapter
                Adapter = new PostHogTelemetryAdapter(
                    host: "https://us.i.posthog.com", 
                    projectApiKey: "phc_wmrmHFqGo6mMECHcqsUBHYJ8RwGWQaM6tnKeKFBM7oWg" // 真实 token
                ),
                Requests = new TelemetryRequest[]
                {
                    TelemetryRequest.BasicUsage(
                        ModSettingsText.Literal("发送版本、平台、语言和匿名安装 ID，用于统计兼容性问题范围。")),
                    TelemetryRequest.Custom(
                        "balance_event",
                        ModSettingsText.Literal("发送本 Mod 的平衡性事件，例如挑战选择和重掷次数。")),
                    TelemetryRequest.Diagnostics(
                        ModSettingsText.Literal("发送异常和诊断上下文，用于定位崩溃。")),
                    TelemetryRequest.RunHistory(
                        ModSettingsText.Literal("发送已结束跑局的原版数据，用于分析平衡性。"))
                }
            });

            Client = TelemetryApi.GetClient(ApplicantId);
        }

        // ========= 下面的方法都不用改 =========
        public static void CaptureChallengeSelected(string challengeId, bool hardMode)
        {
            Client?.CapturePayload(
                eventName: "challenge.selected",
                requestId: "balance_event",
                payload: new JsonObject
                {
                    ["challenge_id"] = challengeId,
                    ["hard_mode"] = hardMode,
                },
                properties: new Dictionary<string, object?>
                {
                    ["challenge_id"] = challengeId,
                    ["hard_mode"] = hardMode,
                });
        }

        public static void CaptureDraftReroll(int rerollIndex)
        {
            Client?.Capture(
                eventName: "draft.rerolled",
                requestId: "balance_event",
                properties: new Dictionary<string, object?>
                {
                    ["reroll_index"] = rerollIndex,
                });
        }

        public static void CaptureExceptionSafe(Exception ex, string context = "")
        {
            Client?.CaptureException(
                ex,
                new Dictionary<string, object?>
                {
                    ["context"] = context,
                });
        }

        public static void UploadRunHistory(JsonNode runHistoryJson, string source = "manual")
        {
            TelemetryApi.CaptureVanillaRunHistory(
                ApplicantId,
                runHistoryJson,
                applicantPayload: new JsonObject { ["source"] = source },
                properties: new Dictionary<string, object?>
                {
                    ["payload_kind"] = "imported_run_history",
                });
        }
    }
}