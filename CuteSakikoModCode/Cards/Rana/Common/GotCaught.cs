using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class GotCaught() : CuteRanaCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar("BaseBlock", 2m, ValueProp.Move),
        new BlockVar("ExtraBlock", 7m, ValueProp.Move)
    };

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (CombatState == null) return false;
            return CombatState.HittableEnemies.Any(e => e.Monster != null && e.Monster.IntendsToAttack);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var baseBlock = DynamicVars["BaseBlock"].BaseValue;
        await CreatureCmd.GainBlock(Owner.Creature, baseBlock, ValueProp.Move, cardPlay);

        bool anyEnemyIntendsToAttack = CombatState?.HittableEnemies
            .Any(e => e.Monster != null && e.Monster.IntendsToAttack) ?? false;

        if (anyEnemyIntendsToAttack)
        {
            var extraBlock = DynamicVars["ExtraBlock"].BaseValue;
            await CreatureCmd.GainBlock(Owner.Creature, extraBlock, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ExtraBlock"].UpgradeValueBy(3m);
    }
}