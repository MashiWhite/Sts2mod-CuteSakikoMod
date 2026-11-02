using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

/// <summary>
///     芭菲大爆炸：食用 2 杯抹茶芭菲，对所有敌人造成 34 点伤害。
///     升级：34 → 40。
///     数值依据：卡牌数值基准表 §1.3「芭菲卡可以略高，但不是顶格」——
///     30~38 伤 × 0.5 = 15~19 VP 自身部分 + 2 杯 × 9 = 18 VP，总预算 33~37，
///     自身部分落在金卡 2c 档中位而非顶格。（2026-11-01 用户裁定，原为 40/48）
/// </summary>
public class ParfaitBigBang : CuteRanaCard, CuteRanaCard.IEatParfaitCard
{
    public ParfaitBigBang() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(34m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword()); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先扣芭菲（不足按关键词定义无法打出——费用校验由关键词系统承担）
        await MatchaParfait.SimulateParfaitEaten(Owner, 2, choiceContext);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m); // 34 → 40
    }

    /// <summary>食用 2 杯抹茶芭菲。杯数不足时由基类 IsPlayable 拦下，牌不可打出。</summary>
    public int GetParfaitConsumeCount()
    {
        return 2;
    }
}
