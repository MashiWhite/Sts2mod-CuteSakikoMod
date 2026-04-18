using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

[Pool(typeof(CuteSakiCardPool))]
public class NightRebirth : CustomCardModel
{
    private int _threshold = 4; // 升级前：8层压力换1能量

    public NightRebirth() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // 金闪闪提示：当有压力时卡牌高亮
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var pressure = Owner.Creature.GetPower<PressurePower>();
            return pressure != null && pressure.Amount > 0;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
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
        var energyGain = pressureAmount / _threshold; // 整数除法，向下取整
        if (energyGain > 0) await PlayerCmd.GainEnergy(energyGain, Owner);
        // 消耗所有压力
        await PowerCmd.ModifyAmount(pressure, -pressureAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        _threshold = 2; // 升级后：5层压力换1能量
    }
}