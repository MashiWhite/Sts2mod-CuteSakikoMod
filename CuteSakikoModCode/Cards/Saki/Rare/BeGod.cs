using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

[Pool(typeof(CuteSakiCardPool))]
public class BeGod : CustomCardModel
{
    public BeGod() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars => System.Array.Empty<DynamicVar>();
    

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int layers = IsUpgraded ? 2 : 1;
        await PowerCmd.Apply<BeGodPower>(Owner.Creature, layers, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
    }
}