using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Events;

namespace CuteSakikoMod.CuteSakikoModCode.Patches
{
    public class StarAnonEventPatcher : IPatchMethod
    {
        // 每局游戏的访问计数器（附着在 RunState 上）
        private static readonly SavedAttachedState<RunState, int> _eventCounter =
            new("StarAnonEventPatch_EventCounter", () => 0);

        public static string PatchId => "star_anon_event_guarantee";
        public static string Description => "Forces StarAnonEvent to appear within the first three events.";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return new[]
            {
                new ModPatchTarget(typeof(ActModel), "PullNextEvent")
            };
        }

        // 前缀：在原始方法执行前拦截
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(ActModel __instance, RunState runState, ref EventModel __result)
        {
            if (runState == null) return true; // 安全回退

            int count = _eventCounter.GetValueOrDefault(runState);
            if (count < 3)
            {
                // 强制替换为 StarAnonEvent
                __result = ModelDb.Event<StarAnonEvent>().ToMutable();
                _eventCounter.Update(runState, c => c + 1);
                // 跳过原始方法
                return false;
            }

            // 正常调用原始方法
            return true;
        }
    }
}