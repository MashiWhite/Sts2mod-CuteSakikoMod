using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class SymbolThree() : CuteSakikoModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Weak", 1),
        new BlockVar(14m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var pressure = Owner.Creature.GetPower<PressurePower>();
            return pressure != null && pressure.Amount >= 2;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var pressure = Owner.Creature.GetPower<PressurePower>();
        if (pressure != null && pressure.Amount >= 2)
        {
            // 消耗2层压力
            await PowerCmd.ModifyAmount(choiceContext, pressure, -2, Owner.Creature, this);

            var read = DynamicVars["Weak"].IntValue;
            await PowerCmd.Apply<WeakPower>(choiceContext, CombatState.HittableEnemies, read, Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Weak"].UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}