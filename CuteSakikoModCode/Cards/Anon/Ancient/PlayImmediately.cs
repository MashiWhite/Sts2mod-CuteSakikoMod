using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Ancient;

public class PlayImmediately() : CuteAnonCard(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.NoNote.GetModCardKeyword()];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new ("Chords",3)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PlayImmediatelyPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        // 演奏所有储存的和弦
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
        {
            await guitar.TriggerAllStoredChordsKeepNotes(choiceContext);
        }
        var chords = DynamicVars["Chords"].BaseValue;

        // 获得可叠层的“即刻演奏”，层数 = Chords
        await PowerCmd.Apply<PlayImmediatelyPower>(choiceContext, Owner.Creature, chords, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Chords"].UpgradeValueBy(2);
    }
}