using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Potions.Saki.Common;

public sealed class AsahiBeer : CuteSakikoModPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Unpowered),
        new PowerVar<PressurePower>(15m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PressurePower>(); }
    }

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target == null || !target.IsAlive)
            return;

        // 造成 15 点伤害
        // ★ 药水伤害：算玩家来源（dealer = Owner.Creature），但不吃加成。
        //   ValueProp.Unpowered = 药水/遗物/能力伤害（原生 FirePotion 同款），
        //   不会被力量、易伤等 IsPoweredAttack 加成影响。
        await DamageCompat.Damage(
            choiceContext,
            target,
            DynamicVars["Damage"].BaseValue,
            ValueProp.Unpowered,
            Owner.Creature, // dealer：算玩家来源
            null,           // 药水没有 CardModel
            null            // 药水没有 CardPlay
        );

        // 给予 15 层压力
        await PressureCmd.Apply(
            choiceContext,
            target,
            DynamicVars["PressurePower"].BaseValue,
            Owner.Creature,
            null
        );
    }
}