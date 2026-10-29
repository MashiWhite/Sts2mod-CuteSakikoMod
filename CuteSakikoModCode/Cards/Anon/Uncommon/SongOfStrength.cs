using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class SongOfStrength() : CuteAnonCard(-1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PerN", 4)
    ];

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.EquippedChords.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var chords = Owner.GetChords();
        if (chords == null) return;

        var chordIds = chords.GetEquippedChordIds();
        if (chordIds.Count == 0) return;

        var attackCount = 0;
        var skillCount = 0;

        foreach (var chordId in chordIds)
            if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                foreach (var type in def.NoteSequence)
                    if (type == CardType.Attack) attackCount++;
                    else if (type == CardType.Skill) skillCount++;

        var x = ResolveEnergyXValue();
        if (x <= 0) return;

        var perN = DynamicVars["PerN"].IntValue;

        var strengthAmount = attackCount / perN * x;
        var dexterityAmount = skillCount / perN * x;

        if (strengthAmount > 0)
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, strengthAmount, Owner.Creature, this);
        if (dexterityAmount > 0)
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, dexterityAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PerN"].UpgradeValueBy(-1m);
    }
}
