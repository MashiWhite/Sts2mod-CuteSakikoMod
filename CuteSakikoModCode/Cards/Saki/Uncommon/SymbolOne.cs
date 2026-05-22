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

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class SymbolOne() : CuteSakikoModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),   // 实际伤害，吃力量等修正
        new DynamicVar("Increase", 6m)        // 每次消耗压力后永久提升的数值
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
        // 1. 消耗压力并永久提升伤害（在攻击前完成，使本次攻击立即享受加成）
        var pressure = Owner.Creature.GetPower<PressurePower>();
        if (pressure != null && pressure.Amount >= 2)
        {
            await PowerCmd.ModifyAmount(choiceContext, pressure, -2, Owner.Creature, this);
            DynamicVars.Damage.BaseValue += DynamicVars["Increase"].BaseValue;
        }

        // 2. 火焰特效（参考 FirePotion）
        var room = NCombatRoom.Instance;
        if (room != null)
            foreach (var enemy in CombatState.HittableEnemies)
                room.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(enemy));

        // 3. 发起攻击（使用 AttackCmd 以触发角色攻击动画和武器特效）
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")   // 可替换为你自己的武器轨迹特效
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["Increase"].UpgradeValueBy(2m);   // 6→8
    }
}