using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Combat.CardTargeting;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class CutesakiTargets
{
    public static readonly TargetType Anyone = 
        CustomTargetType.RegisterSingleTargetType(
            "CuteSakikoMod", 
            "anyone", 
            creature => creature.IsAlive
        );
}