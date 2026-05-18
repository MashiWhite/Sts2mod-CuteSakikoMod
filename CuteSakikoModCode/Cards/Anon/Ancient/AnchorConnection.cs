using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Ancient;

public class AnchorConnection() : CuteAnonCard(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
            if (IsUpgraded) yield return CardKeyword.Innate;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectedChordIds = await ChordSelectCmd.SelectChords(choiceContext, Owner, 2);
        if (selectedChordIds.Count < 2) return; // 用户取消或无有效选择

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        var multiplier = guitar?.GetEffectMultiplier() ?? 1;

        foreach (var chordId in selectedChordIds)
            if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                await def.Effect(choiceContext, Owner.Creature, multiplier);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        AddKeyword(CardKeyword.Innate);
    }
}