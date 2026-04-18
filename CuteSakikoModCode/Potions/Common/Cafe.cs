using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
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

namespace CuteSakikoMod.CuteSakikoModCode.Potions.Common;

[Pool(typeof(CuteSakiPotionPool))]
public sealed class Cafe : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override string? CustomPackedImagePath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PotionsImagePath();

    public override string? CustomPackedOutlinePath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + "outline.png").PotionsImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<PressurePower>(5m) };

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PressurePower>() };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        NCombatRoom.Instance?.PlaySplashVfx(target, new Color("8B4513"));
        await PowerCmd.Apply<PressurePower>(target, DynamicVars["PressurePower"].BaseValue, Owner.Creature, null);
    }
}