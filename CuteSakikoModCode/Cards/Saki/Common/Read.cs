
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class Read() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var pressure = Owner.Creature.GetPower<PressurePower>();
            return pressure != null && pressure.Amount >= 5;
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

        if (pressure == null || pressure.Amount < 5)
            return;

        // 消耗5层压力
        await PowerCmd.ModifyAmount(choiceContext,pressure, -5, Owner.Creature, this);

        var maxHpGain = IsUpgraded ? 2 : 1;
        await CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在逻辑中已处理
    }
}