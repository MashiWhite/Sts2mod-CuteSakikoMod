#if !CSK_GAME_108_OR_NEWER
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Compat;

/// <summary>
/// 0.107 的 AttackCommand.FromCard 只有单参重载（0.108 起才带 CardPlay）。
/// 这里补一个双参扩展，调用点就不用满地图加 #if。
/// </summary>
internal static class DamageCompatExtensions
{
    public static AttackCommand FromCard(this AttackCommand command, CardModel card, CardPlay? cardPlay)
        => command.FromCard(card);
}
#endif
