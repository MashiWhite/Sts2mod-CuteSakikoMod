using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Potions.Saki.Common;

public sealed class Cafe : CuteSakikoModPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PressurePower>(5m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PressurePower>(); }
    }

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        // 手工验证目标合法性（因为原版 AssertValidForTargetedPotion 不理解自定义类型）
        if (target == null || !target.IsAlive)
            return; // 或者抛出异常提示

        NCombatRoom.Instance?.PlaySplashVfx(target, new Color("8B4513"));
        await PowerCmd.Apply<PressurePower>(choiceContext, target, DynamicVars["PressurePower"].BaseValue,
            Owner.Creature, null);
    }
}