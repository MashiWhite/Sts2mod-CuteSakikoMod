using System.Reflection;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;

public static class CuteSakikoModTelemetry
{
    private const string ApplicantId = "CuteSakikoMod";
    private const string BalanceRequestId = "balance_event";
    private static ITelemetryClient Client = null!;
    private static string? _cachedVersion;

    private static readonly Dictionary<string, string> _locCache = new();
    private static readonly HashSet<string> _locMissLogged = new();

    private static I18N I18n => new(
        ApplicantId,
        new[] { "res://CuteSakikoMod/localization" });

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
                "https://us.i.posthog.com",
                "phc_wmrmHFqGo6mMECHcqsUBHYJ8RwGWQaM6tnKeKFBM7oWg"
            ),
            Requests = new[]
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
                        "发送异常和诊断上下文，用于定位崩溃。"))
            }
        });

        Client = TelemetryApi.GetClient(ApplicantId);
    }

    // ==================== 本地化工具 ====================

    public static string LocalizeOrId(string locTable, string id)
    {
        if (string.IsNullOrEmpty(id)) return id;

        var cacheKey = locTable + "|" + id;
        if (_locCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var result = id;
        try
        {
            var dot = id.IndexOf('.');
            var entry = dot >= 0 ? id[(dot + 1)..] : id;

            foreach (var suffix in new[] { ".title", ".name", "" })
                try
                {
                    var key = entry + suffix;
                    var loc = new LocString(locTable, key);
                    if (!loc.Exists()) continue;
                    var text = loc.GetFormattedText();
                    if (!string.IsNullOrWhiteSpace(text) && text != key)
                    {
                        result = text;
                        break;
                    }
                }
                catch
                {
                }
        }
        catch
        {
        }

        _locCache[cacheKey] = result;
        if (result == id && _locMissLogged.Add(cacheKey))
            Entry.Logger.Info($"[Loc] miss: table={locTable}, id={id}");
        return result;
    }

    public static string LocalizeByKey(string locTable, string key)
    {
        if (string.IsNullOrEmpty(locTable) || string.IsNullOrEmpty(key)) return key;
        var cacheKey = "BYKEY|" + locTable + "|" + key;
        if (_locCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var result = key;
        try
        {
            var loc = new LocString(locTable, key);
            if (loc.Exists())
            {
                var text = loc.GetFormattedText();
                if (!string.IsNullOrWhiteSpace(text) && text != key)
                    result = text;
            }
        }
        catch
        {
        }

        _locCache[cacheKey] = result;
        return result;
    }

    public static string LocalizeCard(string id)
    {
        return LocalizeOrId("cards", id);
    }

    public static string LocalizeRelic(string id)
    {
        return LocalizeOrId("relics", id);
    }

    public static string LocalizeMonster(string id)
    {
        return LocalizeOrId("monsters", id);
    }

    public static string LocalizeCharacter(string id)
    {
        return LocalizeOrId("characters", id);
    }

    public static string LocalizePotion(string id)
    {
        return LocalizeOrId("potions", id);
    }

    public static string LocalizePower(string id)
    {
        return LocalizeOrId("powers", id);
    }

    public static string LocalizeAct(string id)
    {
        return LocalizeOrId("acts", id);
    }

    public static string LocalizeModifier(string id)
    {
        var t = LocalizeOrId("modifiers", id);
        if (t != id) return t;
        return LocalizeOrId("relics", id);
    }

    public static string LocalizeEncounter(string id)
    {
        var t = LocalizeOrId("encounters", id);
        if (t != id) return t;
        t = LocalizeOrId("monsters", id);
        if (t != id) return t;
        return id;
    }

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
        Client?.Capture(eventName, BalanceRequestId, WithVersion(properties));
    }

    public static void TrackPayload(string eventName, JsonNode payload, Dictionary<string, object?>? properties = null)
    {
        Client?.CapturePayload(eventName, BalanceRequestId,
            payload, WithVersion(properties));
    }

    // ==================== 通用事件 ====================
    
    /// <summary>TGW 会员卡：还款（右键结清）事件。</summary>
    public static void CaptureTgwRepaid(
        string characterId, int floor,
        int paid, int debtBefore, int debtAfter,
        bool fullyRepaid, int levelBefore, int levelAfter)
    {
        Track("togawa_card.repaid", new Dictionary<string, object?>
        {
            ["character_id"] = characterId,
            ["character_name"] = LocalizeCharacter(characterId),
            ["floor"] = floor,
            ["paid"] = paid,
            ["debt_before"] = debtBefore,
            ["debt_after"] = debtAfter,
            ["fully_repaid"] = fullyRepaid,
            ["level_before"] = levelBefore,
            ["level_after"] = levelAfter
        });
    }

    /// <summary>TGW 会员卡：高消费限制触发事件。</summary>
    public static void CaptureTgwShopLimit(
        string characterId, int floor, int debt, int consecutiveCombats)
    {
        Track("togawa_card.shop_limit", new Dictionary<string, object?>
        {
            ["character_id"] = characterId,
            ["character_name"] = LocalizeCharacter(characterId),
            ["floor"] = floor,
            ["debt"] = debt,
            ["consecutive_combats"] = consecutiveCombats
        });
    }

    public static void CaptureRunStarted(string characterId, int ascension, bool isMultiplayer)
    {
        Track("run.started", new Dictionary<string, object?>
        {
            ["character_id"] = characterId,
            ["character_name"] = LocalizeCharacter(characterId),
            ["ascension"] = ascension,
            ["is_multiplayer"] = isMultiplayer
        });
    }

    public static void CaptureRunEnded(string characterId, int floor, bool victory)
    {
        Track("run.ended", new Dictionary<string, object?>
        {
            ["character_id"] = characterId,
            ["character_name"] = LocalizeCharacter(characterId),
            ["floor"] = floor,
            ["victory"] = victory
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
            ["floor"] = floor
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
            ["floor"] = floor
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
            ["source"] = source
        });
    }

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
            ["floor"] = floor
        };
        if (extra != null)
            foreach (var kv in extra)
                props[kv.Key] = kv.Value;
        Track("room_button.clicked", props);
    }

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

        TrackPayload("event.choices",
            new JsonObject
            {
                ["event_id"] = eventId,
                ["event_name"] = eventName,
                ["options"] = optionList
            },
            new Dictionary<string, object?>
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
                ["option_count"] = optionList.Count
            });
    }

    // ==================== 本地化跑局上传 ====================

    public static void CaptureLocalizedRun(SerializableRun run)
    {
        if (run == null) return;
        try
        {
            var jsonText = SaveManager.ToJson(run);
            if (JsonNode.Parse(jsonText) is not JsonObject root) return;

            root["mod_version"] = ModVersion;

            // ============ players ============
            if (root["players"] is JsonArray players)
                foreach (var pNode in players)
                {
                    if (pNode is not JsonObject player) continue;

                    if (player["character_id"] is JsonValue cid)
                        player["character_name"] = LocalizeCharacter(cid.ToString());

                    AddNamesByIdArray(player, "deck", LocalizeCard);
                    AddNamesByIdArray(player, "relics", LocalizeRelic);
                    AddNamesByIdArray(player, "potions", LocalizePotion);

                    if (player["discovered_cards"] is JsonArray dc)
                        player["discovered_cards_names"] = LocalizeModelIdArray(dc, LocalizeCard);
                    if (player["discovered_enemies"] is JsonArray de)
                        player["discovered_enemies_names"] = LocalizeModelIdArray(de, LocalizeMonster);
                    if (player["discovered_potions"] is JsonArray dp)
                        player["discovered_potions_names"] = LocalizeModelIdArray(dp, LocalizePotion);
                    if (player["discovered_relics"] is JsonArray dr)
                        player["discovered_relics_names"] = LocalizeModelIdArray(dr, LocalizeRelic);
                }

            // ============ events_seen / modifiers ============
            if (root["events_seen"] is JsonArray eventsSeen)
                root["events_seen_names"] = LocalizeModelIdArray(eventsSeen, LocalizeEvent);

            if (root["modifiers"] is JsonArray modifiers)
            {
                var names = new JsonArray();
                foreach (var m in modifiers)
                    if (m is JsonObject mo && mo["id"] is JsonValue mv)
                        names.Add(LocalizeModifier(mv.ToString()));
                    else
                        names.Add("");
                root["modifiers_names"] = names;
            }

            // ============ map_point_history ============
            if (root["map_point_history"] is JsonArray acts)
                foreach (var actNode in acts)
                {
                    if (actNode is not JsonArray mps) continue;
                    foreach (var mpNode in mps)
                    {
                        if (mpNode is not JsonObject mp) continue;

                        // rooms
                        if (mp["rooms"] is JsonArray rooms)
                            foreach (var rNode in rooms)
                            {
                                if (rNode is not JsonObject room) continue;

                                if (room["model_id"] is JsonValue mid)
                                    room["model_id_name"] = LocalizeEncounter(mid.ToString());

                                if (room["monster_ids"] is JsonArray mons)
                                    AddNamesFromStringArray(room, "monster_ids", mons, LocalizeMonster);
                            }

                        // player_stats
                        if (mp["player_stats"] is not JsonArray stats) continue;
                        foreach (var sNode in stats)
                        {
                            if (sNode is not JsonObject stat) continue;

                            AddNamesByIdArray(stat, "cards_gained", LocalizeCard);
                            AddNamesByIdArray(stat, "cards_removed", LocalizeCard);
                            AddNamesByIdArray(stat, "card_choices", LocalizeCard, "card");
                            AddNamesByIdArray(stat, "relic_choices", LocalizeRelic, "relic");
                            AddNamesByIdArray(stat, "potion_choices", LocalizePotion, "potion");

                            AddNamesFromStringArray(stat, "potion_discarded", stat["potion_discarded"] as JsonArray,
                                LocalizePotion);
                            AddNamesFromStringArray(stat, "potion_used", stat["potion_used"] as JsonArray,
                                LocalizePotion);
                            AddNamesFromStringArray(stat, "relics_removed", stat["relics_removed"] as JsonArray,
                                LocalizeRelic);
                            AddNamesFromStringArray(stat, "upgraded_cards", stat["upgraded_cards"] as JsonArray,
                                LocalizeCard);
                            AddNamesFromStringArray(stat, "downgraded_cards", stat["downgraded_cards"] as JsonArray,
                                LocalizeCard);
                            AddNamesFromStringArray(stat, "bought_relics", stat["bought_relics"] as JsonArray,
                                LocalizeRelic);
                            AddNamesFromStringArray(stat, "bought_potions", stat["bought_potions"] as JsonArray,
                                LocalizePotion);
                            AddNamesFromStringArray(stat, "bought_colorless", stat["bought_colorless"] as JsonArray,
                                LocalizeCard);
                            AddNamesFromStringArray(stat, "completed_quests", stat["completed_quests"] as JsonArray,
                                LocalizeRelic);

                            // 附魔卡牌
                            if (stat["cards_enchanted"] is JsonArray enchants)
                            {
                                var names = new JsonArray();
                                foreach (var e in enchants)
                                    if (e is JsonObject eo && eo["card"] is JsonObject ecard &&
                                        ecard["id"] is JsonValue eid)
                                        names.Add(LocalizeCard(eid.ToString()));
                                    else
                                        names.Add("");
                                stat["cards_enchanted_names"] = names;
                            }

                            // 转化卡牌
                            if (stat["cards_transformed"] is JsonArray transforms)
                            {
                                var origNames = new JsonArray();
                                var finalNames = new JsonArray();
                                foreach (var t in transforms)
                                {
                                    if (t is not JsonObject to) continue;
                                    if (to["original_card"] is JsonObject oc && oc["id"] is JsonValue oid)
                                        origNames.Add(LocalizeCard(oid.ToString()));
                                    else origNames.Add("");
                                    if (to["final_card"] is JsonObject fc && fc["id"] is JsonValue fid)
                                        finalNames.Add(LocalizeCard(fid.ToString()));
                                    else finalNames.Add("");
                                }

                                stat["cards_transformed_original_names"] = origNames;
                                stat["cards_transformed_final_names"] = finalNames;
                            }

                            // 远古选择（TextKey 通常为遗物短名）
                            if (stat["ancient_choice"] is JsonArray ancientChoices)
                            {
                                var names = new JsonArray();
                                foreach (var a in ancientChoices)
                                    if (a is JsonObject ao && ao["TextKey"] is JsonValue tk)
                                        names.Add(LocalizeRelic(tk.ToString()));
                                    else names.Add("");
                                stat["ancient_choice_names"] = names;
                            }

                            // 事件选择（title.table + title.key）
                            if (stat["event_choices"] is JsonArray eventChoices)
                            {
                                var names = new JsonArray();
                                foreach (var e in eventChoices)
                                    if (e is JsonObject eo && eo["title"] is JsonObject title
                                                           && title["table"] is JsonValue tv &&
                                                           title["key"] is JsonValue kv)
                                        names.Add(LocalizeByKey(tv.ToString(), kv.ToString()));
                                    else names.Add("");
                                stat["event_choices_names"] = names;
                            }
                        }
                    }
                }

            // ============ acts ============
            if (root["acts"] is JsonArray actsArr)
                foreach (var aNode in actsArr)
                {
                    if (aNode is not JsonObject act) continue;

                    if (act["id"] is JsonValue idv)
                        act["name"] = LocalizeAct(idv.ToString());

                    // 新增：给 acts[].rooms 补古代事件中文名
                    if (act["rooms"] is JsonObject rooms
                        && rooms["ancient_id"] is JsonValue aid)
                        rooms["ancient_name"] = LocalizeEvent(aid.ToString());
                }

            TrackPayload(
                "run_history.localized",
                root,
                new Dictionary<string, object?>
                {
                    ["payload_kind"] = "localized_run_history",
                    ["ascension"] = run.Ascension,
                    ["num_reloads"] = run.NumReloads,
                    ["game_mode"] = run.GameMode.ToString(),
                    ["current_act_index"] = run.CurrentActIndex,
                    ["is_victory"] = run.WinTime > 0
                });

            Entry.Logger.Info($"[Telemetry] Localized run uploaded (mod={ModVersion}, asc={run.Ascension}).");
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[Telemetry] CaptureLocalizedRun failed: {ex.Message}");
        }
    }


    // ==================== 辅助方法 ====================

    private static void AddNamesByIdArray(JsonObject obj, string key, Func<string, string> localizer,
        string? nestedKey = null)
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

    private static void AddNamesFromStringArray(JsonObject obj, string key, JsonArray? source,
        Func<string, string> localizer)
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

    private static JsonArray LocalizeModelIdArray(JsonArray source, Func<string, string> localizer)
    {
        var result = new JsonArray();
        foreach (var item in source)
        {
            var id = item is JsonValue v ? v.ToString() : null;
            result.Add(id != null ? JsonValue.Create(localizer(id)) : JsonValue.Create(""));
        }

        return result;
    }

    public static void CaptureExceptionSafe(Exception ex, string context = "")
    {
        Client?.CaptureException(ex, new Dictionary<string, object?>
        {
            ["context"] = context,
            ["mod_version"] = ModVersion
        });
    }

    public static int GetCurrentFloor(Player player)
    {
        try
        {
            var state = player?.RunState;
            if (state == null) return 0;
            var t = state.GetType();
            var prop = t.GetProperty("TotalFloor") ?? t.GetProperty("CurrentFloor") ?? t.GetProperty("Floor");
            if (prop != null) return Convert.ToInt32(prop.GetValue(state) ?? 0);
        }
        catch
        {
        }

        return 0;
    }
}