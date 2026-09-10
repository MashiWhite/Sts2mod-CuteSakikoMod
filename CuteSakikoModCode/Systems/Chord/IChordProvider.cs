using MegaCrit.Sts2.Core.Entities.Players;
using System.Collections.Generic;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord
{
    /// <summary>
    /// 实现此接口的模型（如遗物、卡牌）可以向音符系统提供可用和弦。
    /// </summary>
    public interface IChordProvider
    {
        IReadOnlyList<string> GetAvailableChordIds(Player player);
    }
}