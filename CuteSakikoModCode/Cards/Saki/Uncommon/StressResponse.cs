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
            yield return new PowerVar<PressurePower>("Pressure", 0m);
            yield return new PressureDamageVar();
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

        // 基础 2 倍，升级后 3 倍
        int multiplier = IsUpgraded ? 3 : 2;
        int damage = layers * multiplier;

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 费用不变，伤害倍数提升，在 OnPlay 和预览中通过 IsUpgraded 控制
    }

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
            int multiplier = card.IsUpgraded ? 3 : 2;
            BaseValue = pressurePower != null ? pressurePower.Amount * multiplier : 0;
        }
    }
}