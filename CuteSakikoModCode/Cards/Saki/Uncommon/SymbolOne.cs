using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class SymbolOne() : CustomCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private decimal _permanentBonus; // 永久累积的额外伤害

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(IsUpgraded ? 8m : 6m, ValueProp.Move),
        new DamageVar("TotalDamage", IsUpgraded ? 8m : 6m, ValueProp.Move)
    ];

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var pressure = Owner.Creature.GetPower<PressurePower>();
            return pressure != null && pressure.Amount >= 2;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var baseDamage = DynamicVars.Damage.BaseValue;
        var totalDamage = baseDamage + _permanentBonus;

        var pressure = Owner.Creature.GetPower<PressurePower>();
        if (pressure != null && pressure.Amount >= 2)
        {
            // 消耗2层压力
            await PowerCmd.ModifyAmount(pressure, -2, Owner.Creature, this);

            var increase = IsUpgraded ? 8m : 6m;
            _permanentBonus += increase;
            DynamicVars["TotalDamage"].BaseValue = baseDamage + _permanentBonus;
        }

        await DamageCmd.Attack(totalDamage)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["TotalDamage"].BaseValue = DynamicVars.Damage.BaseValue + _permanentBonus;
    }
}