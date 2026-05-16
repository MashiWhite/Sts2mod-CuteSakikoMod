using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class StressResponse() : CuteSakikoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new PowerVar<PressurePower>("Pressure", 0m); // 显示当前压力层数
            yield return new StressPerHitDamageVar();                // 战斗内实时每次伤害
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
        var layers = pressure?.Amount ?? 0;
        if (layers <= 0) return;

        // 每次伤害 = ceil(层数*0.25) 
        var damagePerHit =  layers / 4;

        // 消耗所有压力
        await PowerCmd.ModifyAmount(choiceContext, pressure, -layers, Owner.Creature, this);

        var hitCount = IsUpgraded ? 7 : 5;

        // 造成固定次数的随机伤害
        for (var i = 0; i < hitCount; i++)
            await DamageCmd.Attack(damagePerHit)
                .FromCard(this)
                .TargetingRandomOpponents(CombatState)
                .WithHitCount(1)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 次数变为7，已在描述和 OnPlay 中通过 IsUpgraded 处理
    }

    /// <summary>
    /// 动态变量：战斗内显示实际每次伤害 = max(1, 压力层数/4)
    /// </summary>
    private class StressPerHitDamageVar : DynamicVar
    {
        public StressPerHitDamageVar() : base("PerHitDamage", 0m)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            if (card.Owner == null) return;

            var pressurePower = card.Owner.Creature?.GetPower<PressurePower>();
            // 战斗内显示真实值，战斗外保持0（描述由公式接管）
            if (pressurePower != null && card.CombatState != null)
                BaseValue = Math.Max(1, pressurePower.Amount / 4);
        }
    }
}