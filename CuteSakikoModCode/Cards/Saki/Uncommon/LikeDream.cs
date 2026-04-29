using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class LikeDream() : CuteSakikoModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // 无需额外变量，直接操作压力层数
    ];

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
        // 获取当前压力层数
        var pressure = Owner.Creature.GetPower<PressurePower>();
        var currentAmount = pressure?.Amount ?? 0;
        if (currentAmount > 0)
            // 翻倍：增加相同数量的层数
            await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, currentAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：费用减少1（2 → 1）
        EnergyCost.UpgradeBy(-1);
    }
}