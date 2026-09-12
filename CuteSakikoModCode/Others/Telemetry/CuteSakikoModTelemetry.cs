using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
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

        /// <summary>当前 Mod 版本号，从 csproj 的 InformationalVersion 读取。</summary>
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
                catch { _cachedVersion = "unknown"; }
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
                            "发送本 Mod 的平衡性事件（角色、卡牌、遗物、事件选项、跑局统计等）。")),
                    TelemetryRequest.Diagnostics(
                        ModSettingsText.I18N(I18n, "TELEMETRY.DIAGNOSTICS.DESC",
                            "发送异常和诊断上下文，用于定位崩溃。")),
                    // ❌ 不再订阅 RunHistory —— 我们走自己的 balance_event 上传
                }
            });

            Client = TelemetryApi.GetClient(ApplicantId);
        }

        // ==================== 本地化工具 ====================

        public static string LocalizeOrId(string locTable, string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            try
            {
                // ModelId 形如 "CARD.CUTE_SAKIKO_MOD_CARD_XXX"，去掉前缀
                var dot = id.IndexOf('.');
                var entry = dot >= 0 ? id[(dot + 1)..] : id;

                var loc = new LocString(locTable, entry + ".title");
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
        public static string LocalizeAct(string id) => LocalizeOrId("acts", id);

        public static string LocalizeEvent(string id)
        {
            var t = LocalizeOrId("events", id);
            if (t != id) return t;
            return LocalizeOrId("ancients", id);
        }

        // ==================== 统一埋点入口 ====================

        private static Dictionary<string, object?> WithVersion(Dictionary<string, object?>? props)
        {
            var dict = props != null ? new Dictionary<string, object?>(props) : new Dictionary<string, object?>();
            dict["mod_version"] = ModVersion;
            return dict;
        }

        public static void Track(string eventName, Dictionary<string, object?>? properties = null)
        {
            Client?.Capture(eventName: eventName, requestId: BalanceRequestId, properties: WithVersion(properties));
        }

        public static void TrackPayload(string eventName, JsonNode payload, Dictionary<string, object?>? properties = null)
        {
            Client?.CapturePayload(eventName: eventName, requestId: BalanceRequestId,
                payload: payload, properties: WithVersion(properties));
        }

        // ==================== 通用事件 ====================

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

        // ==================== 房间按钮 ====================

        public static void CaptureRoomButtonClicked(
            string roomType, string buttonId, string characterId, int floor,
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
            if (extra != null) foreach (var kv in extra) props[kv.Key] = kv.Value;
            Track("room_button.clicked", props);
        }

        // ==================== 事件选项 ====================

        public static void CaptureEventChoices(
            string eventId, string characterId, int floor,
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
                optionList.Add(new JsonObject { ["key"] = key, ["title"] = title, ["chosen"] = chosen });
                if (chosen) { chosenKeys.Add(key); chosenNames.Add(title); }
                else { skippedKeys.Add(key); skippedNames.Add(title); }
            }

            TrackPayload("event.choices",
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
            string eventId, string characterId, int floor, string optionKey, string optionTitle)
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

        // ==================== 本地化跑局上传 ====================

        /// <summary>
        /// 把一局 SerializableRun 本地化后上传（替代 RitsuLib 的 run_history.completed）。
        /// 事件名：run_history.localized
        /// </summary>
        public static void CaptureLocalizedRun(SerializableRun run)
        {
            if (run == null) return;
            try
            {
                // 1) 用游戏自带序列化，ModelId 会输出为 "CARD.XXX" 字符串
                var jsonText = SaveManager.ToJson(run);
                if (JsonNode.Parse(jsonText) is not JsonObject root) return;

                // 2) 补 Mod 版本号
                root["mod_version"] = ModVersion;

                // 3) 本地化 players
                if (root["players"] is JsonArray players)
                {
                    foreach (var pNode in players)
                    {
                        if (pNode is not JsonObject player) continue;

                        if (player["character_id"] is JsonValue cid)
                            player["character_name"] = LocalizeCharacter(cid.ToString());

                        AddNamesByIdArray(player, "deck", LocalizeCard);
                        AddNamesByIdArray(player, "relics", LocalizeRelic);
                        AddNamesByIdArray(player, "potions", LocalizePotion);
                    }
                }

                // 4) 本地化 map_point_history
                if (root["map_point_history"] is JsonArray acts)
                {
                    foreach (var actNode in acts)
                    {
                        if (actNode is not JsonArray mps) continue;
                        foreach (var mpNode in mps)
                        {
                            if (mpNode is not JsonObject mp) continue;

                            // rooms[].monster_ids
                            if (mp["rooms"] is JsonArray rooms)
                            {
                                foreach (var rNode in rooms)
                                {
                                    if (rNode is not JsonObject room) continue;
                                    if (room["monster_ids"] is JsonArray mons)
                                        AddNamesFromStringArray(room, "monster_ids", mons, LocalizeMonster);
                                }
                            }

                            // player_stats
                            if (mp["player_stats"] is not JsonArray stats) continue;
                            foreach (var sNode in stats)
                            {
                                if (sNode is not JsonObject stat) continue;

                                // 数组元素是 { id: "..." }
                                AddNamesByIdArray(stat, "cards_gained", LocalizeCard);
                                AddNamesByIdArray(stat, "cards_removed", LocalizeCard);

                                // 数组元素是 { card: { id } } / { relic: { id } } / { potion: { id } }
                                AddNamesByIdArray(stat, "card_choices", LocalizeCard, "card");
                                AddNamesByIdArray(stat, "relic_choices", LocalizeRelic, "relic");
                                AddNamesByIdArray(stat, "potion_choices", LocalizePotion, "potion");

                                // 数组元素是纯字符串 ModelId
                                AddNamesFromStringArray(stat, "potion_discarded", stat["potion_discarded"] as JsonArray, LocalizePotion);
                                AddNamesFromStringArray(stat, "potion_used", stat["potion_used"] as JsonArray, LocalizePotion);
                                AddNamesFromStringArray(stat, "relics_removed", stat["relics_removed"] as JsonArray, LocalizeRelic);
                                AddNamesFromStringArray(stat, "upgraded_cards", stat["upgraded_cards"] as JsonArray, LocalizeCard);
                                AddNamesFromStringArray(stat, "downgraded_cards", stat["downgraded_cards"] as JsonArray, LocalizeCard);
                                AddNamesFromStringArray(stat, "bought_relics", stat["bought_relics"] as JsonArray, LocalizeRelic);
                                AddNamesFromStringArray(stat, "bought_potions", stat["bought_potions"] as JsonArray, LocalizePotion);
                                AddNamesFromStringArray(stat, "bought_colorless", stat["bought_colorless"] as JsonArray, LocalizeCard);
                            }
                        }
                    }
                }

                // 5) 本地化 acts
                if (root["acts"] is JsonArray actsArr)
                {
                    foreach (var aNode in actsArr)
                    {
                        if (aNode is not JsonObject act) continue;
                        if (act["id"] is JsonValue idv)
                            act["name"] = LocalizeAct(idv.ToString());
                    }
                }

                // 6) 上传
                TrackPayload(
                    eventName: "run_history.localized",
                    payload: root,
                    properties: new Dictionary<string, object?>
                    {
                        ["payload_kind"] = "localized_run_history",
                        ["ascension"] = run.Ascension,
                        ["num_reloads"] = run.NumReloads,
                        ["game_mode"] = run.GameMode.ToString(),
                        ["current_act_index"] = run.CurrentActIndex,
                        ["is_victory"] = run.WinTime > 0,   // 简化判断，见备注
                    });

                Entry.Logger.Info($"[Telemetry] Localized run uploaded (mod={ModVersion}, asc={run.Ascension}).");
            }
            catch (Exception ex)
            {
                Entry.Logger.Warn($"[Telemetry] CaptureLocalizedRun failed: {ex.Message}");
            }
        }

        // ---- 辅助：数组元素是 { id: "..." } 或 { <nestedKey>: { id: "..." } } ----
        private static void AddNamesByIdArray(JsonObject obj, string key, Func<string, string> localizer, string? nestedKey = null)
        {
            if (obj[key] is not JsonArray arr) return;
            var names = new JsonArray();
            foreach (var item in arr)
            {
                string? id = null;
                if (item is JsonObject o)
                {
                    if (nestedKey != null)
                    {
                        if (o[nestedKey] is JsonObject nest && nest["id"] is JsonValue nv)
                            id = nv.ToString();
                    }
                    else if (o["id"] is JsonValue iv)
                    {
                        id = iv.ToString();
                    }
                }
                names.Add(id != null ? JsonValue.Create(localizer(id)) : JsonValue.Create(""));
            }
            obj[key + "_names"] = names;
        }

        // ---- 辅助：数组元素是纯字符串 ModelId ----
        private static void AddNamesFromStringArray(JsonObject obj, string key, JsonArray? source, Func<string, string> localizer)
        {
            if (source == null) return;
            var names = new JsonArray();
            foreach (var item in source)
            {
                var id = item is JsonValue v ? v.ToString() : null;
                names.Add(id != null ? JsonValue.Create(localizer(id)) : JsonValue.Create(""));
            }
            obj[key + "_names"] = names;
        }

        // ==================== 异常 ====================

        public static void CaptureExceptionSafe(Exception ex, string context = "")
        {
            Client?.CaptureException(ex, new Dictionary<string, object?>
            {
                ["context"] = context,
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
                var prop = t.GetProperty("TotalFloor") ?? t.GetProperty("CurrentFloor") ?? t.GetProperty("Floor");
                if (prop != null) return Convert.ToInt32(prop.GetValue(state) ?? 0);
            }
            catch { }
            return 0;
        }
    }
}