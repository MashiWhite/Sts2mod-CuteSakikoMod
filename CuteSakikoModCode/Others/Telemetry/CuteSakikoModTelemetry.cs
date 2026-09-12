using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Others.Telemetry
{
    public static class CuteSakikoModTelemetry
    {
        private const string ApplicantId = "CuteSakikoMod";
        private const string BalanceRequestId = "balance_event";
        private static ITelemetryClient Client = null!;
        private static string? _cachedVersion;

        private static I18N I18n => new(
            instanceName: ApplicantId,
            fsFolders: new[] { "res://CuteSakikoMod/localization" });

        /// <summary>
        /// 当前 Mod 版本号。
        /// 优先从 mod manifest JSON（与 DLL 同目录）读取 "version" 字段；
        /// 读不到时回退到程序集 InformationalVersion / AssemblyVersion。
        /// </summary>
        /// <summary>
        /// 当前 Mod 版本号，从程序集 InformationalVersion 读取。
        /// 版本号在 csproj 中通过 &lt;InformationalVersion&gt; 指定。
        /// </summary>
        public static string ModVersion
        {
            get
            {
                if (_cachedVersion != null) return _cachedVersion;
                try
                {
                    var asm = Assembly.GetExecutingAssembly();
                    var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                    if (info != null && !string.IsNullOrWhiteSpace(info.InformationalVersion))
                    {
                        var v = info.InformationalVersion;
                        var plus = v.IndexOf('+');
                        if (plus >= 0) v = v[..plus];
                        _cachedVersion = v;
                        return _cachedVersion;
                    }
                    _cachedVersion = asm.GetName().Version?.ToString() ?? "unknown";
                }
                catch
                {
                    _cachedVersion = "unknown";
                }
                return _cachedVersion;
            }
        }

        public static void Register()
        {
            TelemetryRegistry.RegisterApplicant(new TelemetryApplicant
            {
                ApplicantId = ApplicantId,
                OwnerModId = Entry.ModId,
                DisplayName = "CuteSakikoMod",
                DisplayNameText = ModSettingsText.Literal("CuteSakikoMod"),
                Adapter = new PostHogTelemetryAdapter(
                    host: "https://us.i.posthog.com",
                    projectApiKey: "phc_wmrmHFqGo6mMECHcqsUBHYJ8RwGWQaM6tnKeKFBM7oWg"
                ),
                Requests = new TelemetryRequest[]
                {
                    TelemetryRequest.BasicUsage(
                        ModSettingsText.I18N(I18n, "TELEMETRY.BASIC_USAGE.DESC",
                            "发送版本、平台、语言和匿名安装 ID，用于统计兼容性问题范围。")),
                    TelemetryRequest.Custom(
                        BalanceRequestId,
                        ModSettingsText.I18N(I18n, "TELEMETRY.BALANCE.DESC",
                            "发送本 Mod 的平衡性事件（角色、卡牌、遗物、事件选项等），用于分析平衡性。")),
                    TelemetryRequest.Diagnostics(
                        ModSettingsText.I18N(I18n, "TELEMETRY.DIAGNOSTICS.DESC",
                            "发送异常和诊断上下文，用于定位崩溃。")),
                    TelemetryRequest.RunHistoryFiltered(
                        ModSettingsText.I18N(I18n, "TELEMETRY.RUN_HISTORY.DESC",
                            "发送已结束跑局的原版数据，用于分析平衡性。"),
                        captureFilter: context =>
                            context.SourceData is RunEndedEvent runEnded && !runEnded.IsAbandoned)
                }
            });

            Client = TelemetryApi.GetClient(ApplicantId);

            TelemetryRegistry.RegisterContributionProvider(new CuteSakikoVersionContribution());
        }

        // ==================== 本地化工具 ====================

        public static string LocalizeOrId(string locTable, string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            try
            {
                var loc = new LocString(locTable, id + ".title");
                if (loc.Exists())
                {
                    var text = loc.GetFormattedText();
                    if (!string.IsNullOrWhiteSpace(text) && text != loc.LocEntryKey)
                        return text;
                }
            }
            catch { }
            return id;
        }

        public static string LocalizeCard(string id) => LocalizeOrId("cards", id);
        public static string LocalizeRelic(string id) => LocalizeOrId("relics", id);
        public static string LocalizeMonster(string id) => LocalizeOrId("monsters", id);
        public static string LocalizeCharacter(string id) => LocalizeOrId("characters", id);
        public static string LocalizePotion(string id) => LocalizeOrId("potions", id);
        public static string LocalizePower(string id) => LocalizeOrId("powers", id);

        public static string LocalizeEvent(string id)
        {
            var t = LocalizeOrId("events", id);
            if (t != id) return t;
            return LocalizeOrId("ancients", id);
        }

        // ==================== 统一埋点入口 ====================

        private static Dictionary<string, object?> WithVersion(Dictionary<string, object?>? props)
        {
            var dict = props != null
                ? new Dictionary<string, object?>(props)
                : new Dictionary<string, object?>();
            dict["mod_version"] = ModVersion;
            return dict;
        }

        public static void Track(string eventName, Dictionary<string, object?>? properties = null)
        {
            Client?.Capture(
                eventName: eventName,
                requestId: BalanceRequestId,
                properties: WithVersion(properties));
        }

        public static void TrackPayload(string eventName, JsonNode payload, Dictionary<string, object?>? properties = null)
        {
            Client?.CapturePayload(
                eventName: eventName,
                requestId: BalanceRequestId,
                payload: payload,
                properties: WithVersion(properties));
        }

        // ==================== 具体事件 ====================

        public static void CaptureRunStarted(string characterId, int ascension, bool isMultiplayer)
        {
            Track("run.started", new Dictionary<string, object?>
            {
                ["character_id"] = characterId,
                ["character_name"] = LocalizeCharacter(characterId),
                ["ascension"] = ascension,
                ["is_multiplayer"] = isMultiplayer,
            });
        }

        public static void CaptureRunEnded(string characterId, int floor, bool victory)
        {
            Track("run.ended", new Dictionary<string, object?>
            {
                ["character_id"] = characterId,
                ["character_name"] = LocalizeCharacter(characterId),
                ["floor"] = floor,
                ["victory"] = victory,
            });
        }

        public static void CaptureCardPlayed(string cardId, string characterId, int floor)
        {
            Track("card.played", new Dictionary<string, object?>
            {
                ["card_id"] = cardId,
                ["card_name"] = LocalizeCard(cardId),
                ["character_id"] = characterId,
                ["character_name"] = LocalizeCharacter(characterId),
                ["floor"] = floor,
            });
        }

        public static void CaptureRelicObtained(string relicId, string characterId, int floor)
        {
            Track("relic.obtained", new Dictionary<string, object?>
            {
                ["relic_id"] = relicId,
                ["relic_name"] = LocalizeRelic(relicId),
                ["character_id"] = characterId,
                ["character_name"] = LocalizeCharacter(characterId),
                ["floor"] = floor,
            });
        }

        public static void CaptureCardObtained(string cardId, string characterId, int floor, string source)
        {
            Track("card.obtained", new Dictionary<string, object?>
            {
                ["card_id"] = cardId,
                ["card_name"] = LocalizeCard(cardId),
                ["character_id"] = characterId,
                ["character_name"] = LocalizeCharacter(characterId),
                ["floor"] = floor,
                ["source"] = source,
            });
        }

        // ==================== 房间按钮点击 ====================

        public static void CaptureRoomButtonClicked(
            string roomType,
            string buttonId,
            string characterId,
            int floor,
            Dictionary<string, object?>? extra = null)
        {
            var props = new Dictionary<string, object?>
            {
                ["room_type"] = roomType,
                ["button_id"] = buttonId,
                ["character_id"] = characterId,
                ["character_name"] = LocalizeCharacter(characterId),
                ["floor"] = floor,
            };
            if (extra != null)
                foreach (var kv in extra) props[kv.Key] = kv.Value;

            Track("room_button.clicked", props);
        }

        // ==================== 事件选项 ====================

        public static void CaptureEventChoices(
            string eventId,
            string characterId,
            int floor,
            IEnumerable<(string key, string title, bool chosen)> choices)
        {
            var eventName = LocalizeEvent(eventId);
            var optionList = new JsonArray();
            var chosenKeys = new List<string>();
            var chosenNames = new List<string>();
            var skippedKeys = new List<string>();
            var skippedNames = new List<string>();

            foreach (var (key, title, chosen) in choices)
            {
                optionList.Add(new JsonObject
                {
                    ["key"] = key,
                    ["title"] = title,
                    ["chosen"] = chosen,
                });
                if (chosen)
                {
                    chosenKeys.Add(key);
                    chosenNames.Add(title);
                }
                else
                {
                    skippedKeys.Add(key);
                    skippedNames.Add(title);
                }
            }

            TrackPayload(
                eventName: "event.choices",
                payload: new JsonObject
                {
                    ["event_id"] = eventId,
                    ["event_name"] = eventName,
                    ["options"] = optionList,
                },
                properties: new Dictionary<string, object?>
                {
                    ["event_id"] = eventId,
                    ["event_name"] = eventName,
                    ["character_id"] = characterId,
                    ["character_name"] = LocalizeCharacter(characterId),
                    ["floor"] = floor,
                    ["chosen_keys"] = string.Join(",", chosenKeys),
                    ["chosen_names"] = string.Join(",", chosenNames),
                    ["skipped_keys"] = string.Join(",", skippedKeys),
                    ["skipped_names"] = string.Join(",", skippedNames),
                    ["option_count"] = optionList.Count,
                });
        }

        public static void CaptureEventOptionSkipped(
            string eventId,
            string characterId,
            int floor,
            string optionKey,
            string optionTitle)
        {
            Track("event.option.skipped", new Dictionary<string, object?>
            {
                ["event_id"] = eventId,
                ["event_name"] = LocalizeEvent(eventId),
                ["character_id"] = characterId,
                ["character_name"] = LocalizeCharacter(characterId),
                ["floor"] = floor,
                ["option_key"] = optionKey,
                ["option_title"] = optionTitle,
            });
        }

        // ==================== 异常与跑局上传 ====================

        public static void CaptureExceptionSafe(Exception ex, string context = "")
        {
            Client?.CaptureException(
                ex,
                new Dictionary<string, object?>
                {
                    ["context"] = context,
                    ["mod_version"] = ModVersion,
                });
        }

        public static void UploadRunHistory(JsonNode runHistoryJson, string source = "manual")
        {
            TelemetryApi.CaptureVanillaRunHistory(
                ApplicantId,
                runHistoryJson,
                applicantPayload: new JsonObject
                {
                    ["source"] = source,
                    ["mod_version"] = ModVersion,
                },
                properties: new Dictionary<string, object?>
                {
                    ["payload_kind"] = "imported_run_history",
                    ["mod_version"] = ModVersion,
                });
        }

        // ==================== 工具方法 ====================

        public static int GetCurrentFloor(MegaCrit.Sts2.Core.Entities.Players.Player player)
        {
            try
            {
                var state = player?.RunState;
                if (state == null) return 0;
                var t = state.GetType();
                var prop = t.GetProperty("TotalFloor")
                           ?? t.GetProperty("CurrentFloor")
                           ?? t.GetProperty("Floor");
                if (prop != null)
                    return Convert.ToInt32(prop.GetValue(state) ?? 0);
            }
            catch { }
            return 0;
        }
    }

    /// <summary>
    /// 给所有 RunHistory 事件附加 Mod 版本号。
    /// </summary>
    public sealed class CuteSakikoVersionContribution : ITelemetryContributionProvider
    {
        public string ContributorModId => Entry.ModId;
        public string ContributionId => "version_context";
        public TelemetryDataCategory Category => TelemetryDataCategory.RunHistory;
        public TelemetryContributionVisibility Visibility =>
            TelemetryContributionVisibility.PrivateToApplicant;

        public JsonNode? Build(TelemetryContributionContext context)
        {
            return new JsonObject
            {
                ["mod_version"] = CuteSakikoModTelemetry.ModVersion,
                ["event_name"] = context.EventName,
            };
        }
    }
}