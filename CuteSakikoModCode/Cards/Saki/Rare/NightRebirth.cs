using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class NightRebirth() : CuteSakikoModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 升级前 4 层压力换 1 能量，升级后 2 层换 1（数值与描述一致，旧注释"8层/5层"是笔误）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Threshold", 4)
    ];

    // 金闪闪提示：当有压力时卡牌高亮
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var pressure = Owner.Creature.GetPower<PressurePower>();
            return pressure != null && pressure.Amount > 0;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pressure = Owner.Creature.GetPower<PressurePower>();
        if (pressure == null || pressure.Amount <= 0) return;

        var pressureAmount = pressure.Amount;
        var energyGain = (int)(pressureAmount / DynamicVars["Threshold"].BaseValue); // 整数除法，向下取整
        if (energyGain > 0) await PlayerCmd.GainEnergy(energyGain, Owner);
        // 消耗所有压力
        await PowerCmd.ModifyAmount(choiceContext, pressure, -pressureAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Threshold"].UpgradeValueBy(-2m); // 4 → 2
    }
}