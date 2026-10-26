using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord;

public static class ChordExtensions
{
    /// <summary>获取已挂载的和弦组件，不存在返回 null。</summary>
    public static ChordStorageCapability? Chords(this AbstractModel model)
        => model.Capability<ChordStorageCapability>();

    /// <summary>存在则返回，不存在则挂载后返回。</summary>
    public static ChordStorageCapability GetOrCreateChords(this AbstractModel model)
        => model.GetOrCreateCapability<ChordStorageCapability>();

    /// <summary>
    ///     从玩家的遗物里找带和弦组件的那个，找不到返回 null。
    ///     多个遗物都带组件时，取 ChordPriority 最高的那个（与 ChordStorageCapability.IsActive 同一口径）。
    /// </summary>
    public static ChordStorageCapability? GetChords(this Player? player)
    {
        if (player?.Relics == null) return null;

        ChordStorageCapability? best = null;
        var bestPriority = int.MinValue;
        foreach (var relic in player.Relics)
        {
            var cap = relic.Capability<ChordStorageCapability>();
            if (cap == null) continue;

            var priority = cap.Config.ChordPriority;
            if (priority > bestPriority)
            {
                bestPriority = priority;
                best = cap;
            }
        }

        return best;
    }
}