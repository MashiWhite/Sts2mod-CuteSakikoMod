using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class FreshStart : CuteAnonCard
{
    public FreshStart() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }
    
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Innate,CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(8m, ValueProp.Move);
            yield return new CardsVar(2);
        }
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        if (cardPlay.Target != null)  await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this,cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 8 → 12
        DynamicVars.Cards.UpgradeValueBy(1m); // 2 → 3
    }
}