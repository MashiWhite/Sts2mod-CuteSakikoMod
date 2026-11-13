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
        new DamageVar(15m, ValueProp.Move),
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
        await DamageCompat.Damage(
            choiceContext,
            target,
            DynamicVars["Damage"].BaseValue,
            ValueProp.Move,
            null, // 药水没有 CardModel，传 null
            null // 药水没有 CardPlay，传 null
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