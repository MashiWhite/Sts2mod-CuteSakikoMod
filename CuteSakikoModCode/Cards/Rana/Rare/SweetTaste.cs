using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

public class SweetTaste : CuteRanaCard
{
    /// <summary>
    ///     本牌依赖「累计食用芭菲数」成长，属于抹茶芭菲体系的一部分。
    ///     ⚠️ 必须声明 Parfait 关键词：挑食（PickyEater）按关键词判断"是否芭菲牌"，
    ///        漏声明会被当成无关牌弃掉（2026-11-07 用户实机发现）。
    /// </summary>
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CutesakiKeywords.Parfait.GetModCardKeyword()];

    public SweetTaste() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 基础伤害 10，每食用 1 杯芭菲提升 3 点
            yield return new CalculationBaseVar(10m);
            yield return new ExtraDamageVar(3m);
            yield return new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((card, target) =>
                {
                    // 获取抹茶芭菲遗物，读取累计食用计数
                    var parfait = card.Owner?.Relics.OfType<MatchaParfait>().FirstOrDefault();
                    if (parfait == null) return 0m;
                    return parfait.TotalConsumedThisCombat;
                });
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 基础伤害 10 → 13
        var calcBase = DynamicVars["CalculationBase"];
        calcBase.UpgradeValueBy(3m);

        // 每杯额外伤害 3 → 6
        var extraDamage = DynamicVars["ExtraDamage"];
        extraDamage.UpgradeValueBy(3m);
    }
}