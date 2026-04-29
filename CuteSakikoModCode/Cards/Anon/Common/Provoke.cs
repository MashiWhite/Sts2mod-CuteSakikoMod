using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class Provoke() : CuteAnonCard(2, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 未升级：18 格挡，3 力量
            yield return new BlockVar(18m, ValueProp.Move);
            yield return new PowerVar<StrengthPower>(3m);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        TriggerBanter();

        // 1. 自身获得格挡
        var blockAmount = DynamicVars.Block.IntValue;
        await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, null);

        // 2. 使目标敌人获得力量
        var strengthAmount = DynamicVars["StrengthPower"].IntValue;
        await PowerCmd.Apply<StrengthPower>(choiceContext, cardPlay.Target, strengthAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 格挡 18 → 22（增加 4）
        DynamicVars.Block.UpgradeValueBy(4m);
        // 力量 3 → 2（减少 1）
        DynamicVars["StrengthPower"].UpgradeValueBy(-1m);
    }
}