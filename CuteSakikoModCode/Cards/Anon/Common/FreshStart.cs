using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class FreshStart : CuteAnonCard
{
    [SavedProperty] private bool _hasBeenPlayedThisCombat;

    public FreshStart() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(8m, ValueProp.Move);
            yield return new CardsVar(2);
        }
    }

    protected override bool ShouldGlowGoldInternal =>
        // 尚未打出时发光，提示玩家首次打出有额外收益
        !_hasBeenPlayedThisCombat;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        if (cardPlay.Target != null) await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage, this);

        if (!_hasBeenPlayedThisCombat)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
            _hasBeenPlayedThisCombat = true;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 8 → 12
        DynamicVars.Cards.UpgradeValueBy(1m); // 2 → 3
    }
}