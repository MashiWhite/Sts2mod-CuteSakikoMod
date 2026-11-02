using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

/// <summary>
///     猫抓完就撒盐：造成 8 点伤害。
///     本场战斗每打出过 1 张猫咪，本牌伤害 +2。
///     升级：8 → 10，每张 +2 → +4。
///     计数来源：NekoCounterManager（战斗作用域单例，ModelDb.Singleton 访问）。
/// </summary>
public class SaltAfterScratch : CuteRanaCard
{
    public SaltAfterScratch() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(8m),
        new ExtraDamageVar(2m),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((card, _) =>
            {
                var ownerPlayer = card.Owner;
                if (ownerPlayer == null) return 0m;
                return NekoCounterManager.Instance.GetCount(ownerPlayer);
            })
    ];

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
        DynamicVars["CalculationBase"].UpgradeValueBy(2m); // 8 → 10
        DynamicVars["ExtraDamage"].UpgradeValueBy(2m);     // +2 → +4
    }
}
