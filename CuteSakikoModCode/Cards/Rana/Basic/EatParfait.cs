using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Ancient;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Basic;

[RegisterArchaicToothTranscendence(typeof(StormInhale))]
[RegisterCharacterStarterCard(typeof(CuteRana), 1, Order = 2)]
public class EatParfait() : CuteRanaCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self), CuteRanaCard.IEatParfaitCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CutesakiKeywords.Parfait.GetModCardKeyword()];
    
    public int GetParfaitConsumeCount() => 2; // 固定消耗2杯

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var parfait = Owner.Relics.OfType<MatchaParfait>().FirstOrDefault();
        if (parfait != null)
            MatchaParfait.RemoveCharges(parfait, 2);

        int healAmount = DynamicVars["Heal"].IntValue;
        await CreatureCmd.Heal(Owner.Creature, healAmount);
    }

    protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(3m);
}