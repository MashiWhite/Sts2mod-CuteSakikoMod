using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class PlayTogether() : CuteAnonCard(-1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ExtraTimes", 2)
    ];

    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.EquippedChords.GetModCardKeyword()); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var chords = Owner.GetChords();
        if (chords == null) return;

        var x = ResolveEnergyXValue();
        var times = x + DynamicVars["ExtraTimes"].IntValue;
        if (times <= 0) return;

        var equipped = chords.GetEquippedChordIds();
        if (equipped.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        for (var i = 0; i < times; i++)
        {
            if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead)
                break;

            await ChordNoteSystem.PlayChordAsync(Owner, rng.NextItem(equipped), choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ExtraTimes"].UpgradeValueBy(2m);
    }
}
