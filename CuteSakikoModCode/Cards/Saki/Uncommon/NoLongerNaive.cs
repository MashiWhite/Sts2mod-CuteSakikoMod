using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class NoLongerNaive() : CuteSakikoModCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new CalculationBaseVar(6m);   // 基础伤害
            yield return new ExtraDamageVar(3m);        // 每张遗忘牌加成
            yield return new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((card, target) =>
                {
                    var owner = card.Owner;
                    if (owner == null) return 0m;
                    var forgetPile = ForgetCardPile.Get(owner);
                    return forgetPile?.Cards.Count ?? 0;
                });
        }
    }
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Memory.GetModCardKeyword()];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CalculationBase"].UpgradeValueBy(3m);  // 10 → 13
        DynamicVars["ExtraDamage"].UpgradeValueBy(2m);      // 3 → 5
    }
}