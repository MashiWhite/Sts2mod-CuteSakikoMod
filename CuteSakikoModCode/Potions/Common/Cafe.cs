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
using STS2RitsuLib.Combat.CardTargeting;

namespace CuteSakikoMod.CuteSakikoModCode.Potions.Common;

public sealed class Cafe : CuteSakikoModPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => CustomTargetType.Anyone;


    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<PressurePower>(5m) };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PressurePower>(); }
    }

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        NCombatRoom.Instance?.PlaySplashVfx(target, new Color("8B4513"));
        await PowerCmd.Apply<PressurePower>(choiceContext, target, DynamicVars["PressurePower"].BaseValue,
            Owner.Creature, null);
    }
}