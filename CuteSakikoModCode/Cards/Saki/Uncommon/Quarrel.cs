using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class Quarrel() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
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
        if (cardPlay.Target == null) return;

        var selfPressureGain = IsUpgraded ? 10 : 5;
        var enemyPressureGain = IsUpgraded ? 15 : 10;

        // 自身增加压力
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, selfPressureGain, Owner.Creature, this);

        // 给选中的敌人增加压力
        await PowerCmd.Apply<PressurePower>(choiceContext, cardPlay.Target, enemyPressureGain, Owner.Creature, this);

        // 下回合自身扣除等量压力（无论是否升级都生效）
        await PowerCmd.Apply<QuarrelSelfPower>(choiceContext, Owner.Creature, selfPressureGain, Owner.Creature, this);

        // 只有未升级时，敌人才会在下回合扣除压力
        if (!IsUpgraded)
            await PowerCmd.Apply<QuarrelEnemyPower>(choiceContext, cardPlay.Target, enemyPressureGain, Owner.Creature,
                this);
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在 OnPlay 中通过 IsUpgraded 处理
    }
}