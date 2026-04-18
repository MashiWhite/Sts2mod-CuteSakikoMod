using BaseLib.Abstracts;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Ancient;

[Pool(typeof(CuteSakiRelicPool))]
public sealed class Mask : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override string BigIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").BigRelicImagePath();

    public override string PackedIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").RelicImagePath();

    protected override string PackedIconOutlinePath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + "_outline.png").RelicImagePath();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PressurePower>(); }
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Creature.Side) return;

        var pressure = Owner.Creature.GetPower<PressurePower>();
        if (pressure == null || pressure.Amount <= 0) return;

        var currentPressure = pressure.Amount;
        var halfPressure = currentPressure / 2;
        if (halfPressure <= 0) return;

        // 减少一半压力
        await PowerCmd.ModifyAmount(pressure, -halfPressure, Owner.Creature, null);

        // 对全体敌人造成等量伤害
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), combatState.Enemies, halfPressure, ValueProp.Move,
            Owner.Creature, null);

        Flash();
    }
}