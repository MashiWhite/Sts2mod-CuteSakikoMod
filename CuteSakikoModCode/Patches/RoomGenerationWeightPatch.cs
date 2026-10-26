using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using CuteSakikoMod.CuteSakikoModCode.Encounters;
using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Events.Ancient;
using CuteSakikoMod.CuteSakikoModCode.Others.Config;

namespace CuteSakikoMod.CuteSakikoModCode.Patches
{
    // ★ 拦截 RunManager.GenerateRooms，保证在所有 Act 初始化完毕后统一修改
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
    [HarmonyPriority(Priority.Low)] 
    public static class RoomGenerationWeightPatch
    {
        public static void Postfix(RunManager __instance)
        {
            var state = __instance.DebugOnlyGetState();
            if (state == null) return;

            // 获取当前跑局的 RNG 保证多人同步
            var rngState = state.Rng.UpFront; 

            // 遍历所有章节（Act）
            for (int i = 0; i < state.Acts.Count; i++)
            {
                var act = state.Acts[i];
                
                // 反射获取 Act 内部的 RoomSet
                var roomsField = AccessTools.Field(typeof(ActModel), "_rooms");
                if (roomsField?.GetValue(act) is not RoomSet rooms) continue;

                // ==========================================
                // 1. 先古之民权重
                // ==========================================
                if (ModConfig.UseModAncientWeight && ModConfig.ModAncientWeight > 0)
                {
                    if (rngState.NextFloat() < ModConfig.ModAncientWeight)
                    {
                        var modAncients = act.GetUnlockedAncients(state.UnlockState)
                            .Where(a => a is CuteSakikoAncientEvent)
                            .ToList();

                        if (modAncients.Count > 0)
                        {
                            // ★ 只能存 Canonical (原版) 模型，不能 ToMutable()，否则进房间会崩溃
                            rooms.Ancient = modAncients[rngState.NextInt(modAncients.Count)];
                        }
                    }
                }

                // ==========================================
                // 2. Boss 权重（包含进阶10双 Boss）
                // ==========================================
                if (ModConfig.UseModBossWeight && ModConfig.ModBossWeight > 0)
                {
                    var modBosses = act.AllBossEncounters
                        .Where(e => e is CuteEncounters && e.RoomType == RoomType.Boss)
                        .ToList();

                    if (modBosses.Count > 0)
                    {
                        // 处理第一个 Boss
                        if (rngState.NextFloat() < ModConfig.ModBossWeight)
                        {
                            rooms.Boss = modBosses[rngState.NextInt(modBosses.Count)];
                        }

                        // 处理第二个 Boss（只有在进阶 10 且是最后一幕时，原版才会生成 SecondBoss）
                        if (rooms.HasSecondBoss && rooms.SecondBoss != null)
                        {
                            // 过滤掉已经被第一个 Boss 抽走的
                            var availableForSecond = modBosses.Where(b => b.Id != rooms.Boss.Id).ToList();

                            if (availableForSecond.Count > 0 && rngState.NextFloat() < ModConfig.ModBossWeight)
                            {
                                rooms.SecondBoss = availableForSecond[rngState.NextInt(availableForSecond.Count)];
                            }
                            else if (availableForSecond.Count == 0 && rooms.Boss is not CuteEncounters && rngState.NextFloat() < ModConfig.ModBossWeight)
                            {
                                // 模组只有一个 Boss，且第一个 Boss 没抽中我们，第二个 Boss 就给模组
                                rooms.SecondBoss = modBosses[0];
                            }
                        }
                    }
                }

                // ==========================================
                // 3. 事件权重（重新洗牌）
                // ==========================================
                if (ModConfig.UseModEventWeight && ModConfig.ModEventWeight >= 0)
                {
                    var modEvents = rooms.events.Where(e => e is CuteSakikoEvent).ToList();
                    var otherEvents = rooms.events.Where(e => e is not CuteSakikoEvent).ToList();

                    if (modEvents.Count > 0 && otherEvents.Count > 0)
                    {
                        var newEvents = new List<EventModel>();
                        
                        while (modEvents.Count > 0 || otherEvents.Count > 0)
                        {
                            if (modEvents.Count == 0)
                            {
                                newEvents.AddRange(otherEvents);
                                break;
                            }
                            if (otherEvents.Count == 0)
                            {
                                newEvents.AddRange(modEvents);
                                break;
                            }

                            // 按我们配置的占比抽签
                            if (rngState.NextFloat() < ModConfig.ModEventWeight)
                            {
                                int idx = rngState.NextInt(modEvents.Count);
                                newEvents.Add(modEvents[idx]);
                                modEvents.RemoveAt(idx);
                            }
                            else
                            {
                                int idx = rngState.NextInt(otherEvents.Count);
                                newEvents.Add(otherEvents[idx]);
                                otherEvents.RemoveAt(idx);
                            }
                        }

                        rooms.events.Clear();
                        rooms.events.AddRange(newEvents);
                    }
                }
            }
        }
    }
}