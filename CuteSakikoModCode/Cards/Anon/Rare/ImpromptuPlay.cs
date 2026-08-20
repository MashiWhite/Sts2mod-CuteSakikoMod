using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class ImpromptuPlay() : CuteAnonCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, CutesakiKeywords.NoNote.GetModCardKeyword()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var allPiles = new[]
        {
            PileType.Hand.GetPile(Owner),
            PileType.Draw.GetPile(Owner),
            PileType.Discard.GetPile(Owner)
        };

        var chordCards = allPiles
            .Where(p => p != null)
            .SelectMany(p => p.Cards)
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Chord.GetModCardKeyword()))
            .ToList();

        // 收集所有要演奏的和弦ID
        var chordIds = new List<string>();
        foreach (var card in chordCards)
        {
            var chordId = (card as CuteAnonCard)?.ChordId;
            if (!string.IsNullOrEmpty(chordId) && ChordManager.AllChords.ContainsKey(chordId))
            {
                chordIds.Add(chordId);
                await CardCmd.Exhaust(choiceContext, card);
            }
        }

        // 一次性演奏所有和弦，共享首次加成
        if (chordIds.Count > 0)
            await guitar.PlaySpecificChords(choiceContext, chordIds);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        AddKeyword(CardKeyword.Innate);
    }
}