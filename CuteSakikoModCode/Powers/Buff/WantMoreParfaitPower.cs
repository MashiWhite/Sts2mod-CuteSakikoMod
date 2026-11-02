using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     我还要吃：每回合你第一次食用抹茶芭菲时，抽 N 张牌（N = 层数）。
///     触发源：MatchaParfait.SimulateParfaitEaten 统一入口。
///     "每回合第一次"由本 Power 的 TriggeredThisTurn 标记，回合开始重置。
/// </summary>
public sealed class WantMoreParfaitPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword()); }
    }

    /// <summary>本回合是否已经触发过（回合开始重置）。</summary>
    public bool TriggeredThisTurn { get; set; }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        TriggeredThisTurn = false;
        await Task.CompletedTask;
    }

    /// <summary>
    ///     由 MatchaParfait.SimulateParfaitEaten 的调用链触发（食用入口）。
    ///     公开静态方法供遗物侧调用，避免 Power 直接依赖遗物类型。
    /// </summary>
    public static async Task OnParfaitConsumedAsync(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel? cardSource)
    {
        var creature = player.Creature;
        if (creature == null) return;
        if (!creature.HasPower<WantMoreParfaitPower>()) return;

        var power = creature.GetPower<WantMoreParfaitPower>();
        if (power == null || power.TriggeredThisTurn) return;

        power.TriggeredThisTurn = true;
        power.Flash();

        var draw = power.Amount;
        if (draw > 0 && choiceContext != null)
            await CardPileCmd.Draw(choiceContext, draw, player);
    }
}
