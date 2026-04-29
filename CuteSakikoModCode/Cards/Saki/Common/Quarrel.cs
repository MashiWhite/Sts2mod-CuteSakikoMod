using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class Quarrel() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 返回压力能力的悬停提示
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            // 如果有其他提示，继续 yield return
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var selfPressureGain = IsUpgraded ? 5 : 10;
        var enemyPressureGain = IsUpgraded ? 15 : 10;

        // 自身增加压力
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, selfPressureGain, Owner.Creature, this);

        // 给选中的敌人增加压力
        await PowerCmd.Apply<PressurePower>(choiceContext, cardPlay.Target, enemyPressureGain, Owner.Creature, this);

        // 临时能力：下回合扣除等量压力
        await PowerCmd.Apply<QuarrelSelfPower>(choiceContext, Owner.Creature, selfPressureGain, Owner.Creature, this);
        await PowerCmd.Apply<QuarrelEnemyPower>(choiceContext, cardPlay.Target, enemyPressureGain, Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
    }
}