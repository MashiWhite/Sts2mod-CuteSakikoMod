using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class NoSheetMusic() : CuteAnonCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.NoNote.GetModKeywordCardKeyword()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var pools = new List<string>();
        pools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
        pools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
        pools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));
        if (pools.Count == 0) return;

        var randomChordId = Owner.RunState.Rng.CombatCardSelection.NextItem(pools);
        await guitar.AddChordToStored(choiceContext, randomChordId);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 -> 1
    }
}