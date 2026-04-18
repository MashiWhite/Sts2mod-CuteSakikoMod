using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

[Pool(typeof(TokenCardPool))]
public class TrueHaruhikage() : CustomCardModel(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 显示击晕能力的悬停提示
            yield return StunIntent.GetStaticHoverTip();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对全体敌人造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 击晕所有敌人
        foreach (var enemy in CombatState.HittableEnemies)
            await CreatureCmd.Stun(enemy);
    }

    protected override void OnUpgrade()
    {
        // 升级：费用 1 → 0，伤害 20 → 30
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Damage.UpgradeValueBy(10m);
    }
}