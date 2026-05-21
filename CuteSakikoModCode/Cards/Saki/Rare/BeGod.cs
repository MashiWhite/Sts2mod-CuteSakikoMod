using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class BeGod() : CuteSakikoModCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var layers = IsUpgraded ? 2 : 1;
        await PowerCmd.Apply<BeGodPower>(choiceContext, Owner.Creature, layers, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
    }
}