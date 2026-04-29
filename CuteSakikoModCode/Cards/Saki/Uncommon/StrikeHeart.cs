using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class StrikeHeart() : CuteSakikoModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 添加动态变量：伤害（基础1点）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<BreakDefendPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 获取目标当前压力层数
        var pressure = cardPlay.Target.GetPower<PressurePower>();
        var pressureAmount = pressure?.Amount ?? 0;

        // 施加等量层数的破防
        if (pressureAmount > 0)
            await PowerCmd.Apply<BreakDefendPower>(choiceContext, cardPlay.Target, pressureAmount, Owner.Creature,
                this);

        // 造成多次1点伤害（使用动态伤害值，受力量加成）
        var hitCount = IsUpgraded ? 10 : 6;
        var damage = DynamicVars.Damage.BaseValue;
        for (var i = 0; i < hitCount; i++)
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级仅增加攻击次数，伤害数值不变
    }
}