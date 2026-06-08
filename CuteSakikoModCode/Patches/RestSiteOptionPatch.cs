using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(RestSiteOption))]
public static class RestSiteOptionPatch
{
    
    [HarmonyPatch(typeof(RestSiteOption))]
    public static class RestSiteOption_IsEnabled_Patch
    {
        private static readonly Dictionary<RestSiteOption, bool> _overrides = new();

        public static void SetEnabled(RestSiteOption option, bool enabled)
        {
            RitsuLibFramework.Logger.Info($"SetEnabled: {option.OptionId} -> {enabled}");
            if (enabled)
                _overrides.Remove(option);
            else
                _overrides[option] = false;
        }

        [HarmonyPatch(nameof(RestSiteOption.IsEnabled), MethodType.Getter)]
        [HarmonyPostfix]
        public static void Postfix(RestSiteOption __instance, ref bool __result)
        {
            if (_overrides.TryGetValue(__instance, out var overridden))
            {
                RitsuLibFramework.Logger.Info($"IsEnabled override: {__instance.OptionId} -> {overridden} (original was {__result})");
                __result = overridden;
            }
        }
    }
    [HarmonyPatch(typeof(SmithRestSiteOption))]
    public static class SmithRestSiteOption_IsEnabled_Patch
    {
        [HarmonyPatch(nameof(RestSiteOption.IsEnabled), MethodType.Getter)]
        [HarmonyPostfix]
        public static void Postfix(SmithRestSiteOption __instance, ref bool __result)
        {
            // 通过反射获取 protected 属性 Owner
            var ownerProp = typeof(RestSiteOption).GetProperty("Owner", BindingFlags.NonPublic | BindingFlags.Instance);
            if (ownerProp == null) return;
            var player = ownerProp.GetValue(__instance) as Player;
            if (player == null) return;

            var guitar = player.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar == null) return;

            if (guitar.NormalOptionUsed && !player.Relics.Any(r => r is MiniatureTent))
            {
                __result = false;
            }
        }
    }
}