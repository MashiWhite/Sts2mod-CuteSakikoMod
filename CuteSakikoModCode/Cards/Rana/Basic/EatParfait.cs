using CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Ancient;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Basic;

[RegisterArchaicToothTranscendence(typeof(StormInhale))]
public class EatParfait()
    : CuteRanaCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self), CuteRanaCard.IEatParfaitCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Parfait.GetModCardKeyword(), CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword()); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(5m)];

    public int GetParfaitConsumeCount()
    {
        return 2;
        // 固定消耗2杯
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var parfait = Owner.Relics.OfType<MatchaParfait>().FirstOrDefault();
        if (parfait != null)
            await MatchaParfait.RemoveCharges(parfait, 2, choiceContext);

        var healAmount = DynamicVars["Heal"].IntValue;
        await CreatureCmd.Heal(Owner.Creature, healAmount);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(3m);
    }
}