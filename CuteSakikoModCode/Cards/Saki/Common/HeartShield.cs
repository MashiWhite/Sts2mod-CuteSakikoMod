using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class HeartShield() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 动态变量：压力层数（基础2层）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PressurePower>(2m)
    ];

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
        var pressureToGain = IsUpgraded ? 4 : 2;

        // 先施加压力
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, pressureToGain, Owner.Creature, this);

        // 获取当前压力层数（已包含刚施加的）
        var pressure = Owner.Creature.GetPower<PressurePower>();
        var currentPressure = pressure?.Amount ?? 0;

        // 获得等量格挡
        if (currentPressure > 0)
        {
            // 临时创建 BlockVar 用于 GainBlock
            var blockVar = new BlockVar(currentPressure, ValueProp.Move);
            await CreatureCmd.GainBlock(Owner.Creature, blockVar, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后压力层数增加，已在 OnPlay 中通过 IsUpgraded 处理
    }
}