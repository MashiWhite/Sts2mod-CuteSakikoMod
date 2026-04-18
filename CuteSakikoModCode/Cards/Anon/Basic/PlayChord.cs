
using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Ancient;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Basic;


public class PlayChord : CuteAnonCard, ITranscendenceCard
{
    public PlayChord() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => System.Array.Empty<DynamicVar>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var relic = Owner.Relics.FirstOrDefault(r => r is AnonGuitar) as AnonGuitar;
        if (relic == null) return;

        await relic.TriggerAllStoredChords(choiceContext);
    }

    
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<PlayImmediately>();
    
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 升级后0费
    }
}