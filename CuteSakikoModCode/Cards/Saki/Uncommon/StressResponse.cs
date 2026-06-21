using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class StressResponse : CuteSakikoModCard
{
    public StressResponse() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new PowerVar<PressurePower>("Pressure", 0m); // 显示当前压力层数
            yield return new PressureDamageVar(); // 动态伤害值 = 压力 × 2
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

        int damage = layers * 2;

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .TargetingRandomOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2c → 1c
    }

    /// <summary>
    /// 动态变量：伤害值 = 压力层数 × 2，战斗中实时预览
    /// </summary>
    private class PressureDamageVar : DynamicVar
    {
        public PressureDamageVar() : base("PressureDamage", 0m)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            if (card.Owner == null) return;

            var pressurePower = card.Owner.Creature?.GetPower<PressurePower>();
            BaseValue = pressurePower != null ? pressurePower.Amount * 2 : 0;
        }
    }
}