using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class InterestingWoman : CuteRanaCard
{
    public InterestingWoman() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<WeakPower>(1m),    // 虚弱层数
        new PowerVar<VulnerablePower>(1m) // 易伤层数
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        int vulnerablePower = DynamicVars["WeakPower"].IntValue;
        int weakAmount = DynamicVars["VulnerablePower"].IntValue;

        
        // 给予虚弱
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, weakAmount, Owner.Creature, this);
        // 给予易伤
        await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, vulnerablePower, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
       DynamicVars["WeakPower"].UpgradeValueBy(1);
       DynamicVars["VulnerablePower"].UpgradeValueBy(1);
    }
}