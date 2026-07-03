using System.Collections.Generic;
using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

public class SweetTaste : CuteRanaCard
{
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
            .FromCard(this,cardPlay)
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