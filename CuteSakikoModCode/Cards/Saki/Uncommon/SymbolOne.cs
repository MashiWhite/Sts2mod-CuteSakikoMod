using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
// 新增：用于 NCombatRoom
// 新增：用于 NGroundFireVfx

// 建议加上，虽然可能已有隐式 using

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class SymbolOne() : CuteSakikoModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private decimal _permanentBonus; // 永久累积的额外伤害

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(IsUpgraded ? 8m : 6m, ValueProp.Move),
        new("TotalDamage", IsUpgraded ? 8m : 6m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
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
            await PowerCmd.ModifyAmount(choiceContext, pressure, -2, Owner.Creature, this);

            var increase = IsUpgraded ? 8m : 6m;
            _permanentBonus += increase;
            DynamicVars["TotalDamage"].BaseValue = baseDamage + _permanentBonus;
        }

        // ================= 新增火焰特效 =================
        // 为每个可攻击的敌人添加地面火焰特效（参考 FirePotion）
        var room = NCombatRoom.Instance;
        if (room != null)
            foreach (var enemy in CombatState.HittableEnemies)
                room.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(enemy));
        // =================================================

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