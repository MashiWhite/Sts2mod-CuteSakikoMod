using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Ancient;
public class StormInhale() : CuteRanaCard(0, CardType.Skill, CardRarity.Ancient, TargetType.Self), CuteRanaCard.IEatParfaitCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword());
        }
    }
    
    public int GetParfaitConsumeCount() => IsUpgraded ? 1 : 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HealVar(5m), new CardsVar(2), new EnergyVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var parfait = Owner.Relics.OfType<MatchaParfait>().FirstOrDefault();
        if (parfait != null)
            await MatchaParfait.RemoveCharges(parfait, GetParfaitConsumeCount(), choiceContext);

        int healAmount = DynamicVars["Heal"].IntValue;
        await CreatureCmd.Heal(Owner.Creature, healAmount);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
        DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}