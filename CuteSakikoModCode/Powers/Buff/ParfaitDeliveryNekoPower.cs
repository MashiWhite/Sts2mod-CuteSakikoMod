using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     送芭菲的猫：你**食用芭菲**时，将 N 张猫咪加入手牌（N = 层数）。
///     2026-11-02 用户改版：触发由「打出猫咪」改为「食用芭菲」，效果由「获得莱芜」改为「加猫到手牌」。
///     升级：召唤的猫咪改为**猫咪+**（由施放时那张卡是否已升级决定，记在 SummonUpgraded）。
///     触发源：MatchaParfait.SimulateParfaitEaten（与 WantMoreParfaitPower 同一个食用入口）。
/// </summary>
public sealed class ParfaitDeliveryNekoPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    /// <summary>
    ///     本 Power 召唤的猫咪是否为已升级形态（猫咪+）。
    ///     由施放它的那张卡在 OnPlay 时写入——卡升级了就写 true。
    ///     ⚠️ 这是战斗内状态，不需要存档（Power 本身随战斗结束销毁）。
    /// </summary>
    public bool SummonUpgraded { get; set; }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword()); }
    }

    /// <summary>
    ///     由 MatchaParfait.SimulateParfaitEaten 调用（食用入口）。
    ///     公开静态方法供遗物侧调用，避免 Power 直接依赖遗物类型。
    ///     ⚠️ 计数口径（2026-11-02 用户定）：**每食用 1 杯 → 加 1 张猫咪**，
    ///        即数量 = 层数 × amount（吃几杯就加几张，不是"每次食用只加一层"）。
    ///        与 WantMoreParfaitPower 不同：这里**不限制**每回合第一次，每次食用都触发。
    /// </summary>
    /// <param name="amount">本次食用的杯数。加猫数量 = 层数 × amount。</param>
    public static async Task OnParfaitConsumedAsync(
        PlayerChoiceContext? choiceContext,
        Player player,
        int amount)
    {
        if (amount <= 0) return;

        var creature = player.Creature;
        if (creature == null) return;
        var power = creature.GetPower<ParfaitDeliveryNekoPower>();
        if (power == null) return;

        power.Flash();

        var count = power.Amount * amount;   // 层数 × 杯数
        if (count <= 0 || choiceContext == null) return;

        await AddNekoToHand(choiceContext, player, count, power.SummonUpgraded);
    }

    /// <summary>
    ///     从猫咪 Token 池随机抽 N 张加入手牌。写法照抄 NekoTreasure（同一套 API 签名）。
    ///     upgraded = true 时把生成的猫逐张升级（即「猫咪+」）。
    ///     ⚠️ GetUnlockedCards 的参数是 (UnlockState, CardMultiplayerConstraint)，
    ///        不是 (ICombatState, Player)；RNG 用 CardGeneration 那条流。
    /// </summary>
    private static async Task AddNekoToHand(
        PlayerChoiceContext choiceContext, Player player, int count, bool upgraded)
    {
        var combatState = player.Creature?.CombatState;
        if (combatState == null) return;

        var nekoKeyword = CutesakiKeywords.Neko.GetModCardKeyword();
        var pool = ModelDb.CardPool<CuteSakikoTokenCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords.Contains(nekoKeyword))
            .ToList();
        if (pool.Count == 0) return;

        var rng = player.RunState.Rng.CombatCardGeneration;
        for (var i = 0; i < count; i++)
        {
            var template = rng.NextItem(pool);
            if (template == null) continue;
            var newCard = combatState.CreateCard(template, player);
            if (upgraded)
            {
                // ★ 生成卡片的正确升级方式（照抄 ManyNeko）：
                //   不能用 CardCmd.Upgrade（那是给已在牌堆里的卡用的），
                //   新建的卡要 UpgradeInternal + FinalizeUpgradeInternal。
                newCard.UpgradeInternal();
                newCard.FinalizeUpgradeInternal();
            }
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, player);
        }
    }
}
