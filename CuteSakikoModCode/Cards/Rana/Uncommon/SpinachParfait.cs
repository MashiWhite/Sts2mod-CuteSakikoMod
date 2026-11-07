using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

/// <summary>
///     大力芭菲（SpinachParfait）：食用 1 杯抹茶芭菲，获得 7 点格挡与 5 层活力。
///     升级：格挡 7 → 9，活力 5 → 8。
///     活力 = 原版机制（下次攻击牌伤害 +X）。
///     ★ 命名：中文「大力芭菲」neta **大力水手（Popeye）吃菠菜**——「大力」取自大力水手，
///       「芭菲」是乐奈的抹茶芭菲资源，抹茶 + 菠菜同为绿叶意象。
///       类名用 **Spinach**（菠菜）而非 Big，是为了避开遗物 **大份抹茶芭菲 BigMatchaParfait**
///       —— 两者都叫 Big 会误导玩家以为是同一个东西。（2026-11-01 用户裁定）
///     2026-11-01 用户裁定：0c 金卡对蓝卡产品线不合理（0 费拿芭菲作主效果、还带 Rare 稀有度，
///     会跟「祈愿芭菲 0 费 Uncommon」抢同一档），故降为 Uncommon 并改用蓝卡数值。
///     VP 核对：格挡 7~9 + 活力 5~8（1 层活力 ≈ 0.8 VP）+ 1 杯 9 VP
///             → 基础约 20 VP / 升级约 24 VP。
/// </summary>
public class SpinachParfait : CuteRanaCard, CuteRanaCard.IEatParfaitCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Parfait.GetModCardKeyword()];

        public SpinachParfait() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
        new PowerVar<VigorPower>(5m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 走 RemoveCharges 才真正扣杯（SimulateParfaitEaten 只记账不扣杯，2026-11-07 修）
        var parfait = Owner.Relics.OfType<MatchaParfait>().FirstOrDefault();
        if (parfait != null)
            await MatchaParfait.RemoveCharges(parfait, GetParfaitConsumeCount(), choiceContext);

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<VigorPower>(
            choiceContext, Owner.Creature, DynamicVars["VigorPower"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);        // 7 → 9
        DynamicVars["VigorPower"].UpgradeValueBy(3m); // 5 → 8
    }

    /// <summary>食用 1 杯抹茶芭菲。杯数不足时由基类 IsPlayable 拦下，牌不可打出。</summary>
    public int GetParfaitConsumeCount()
    {
        return 1;
    }
}
