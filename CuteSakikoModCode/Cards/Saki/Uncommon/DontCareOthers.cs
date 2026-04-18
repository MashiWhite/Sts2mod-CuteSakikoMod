using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;


public class DontCareOthers : CuteSakikoModCard
{
    public DontCareOthers() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }



    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new BlockVar(IsUpgraded ? 7m : 4m, ValueProp.Move);
            yield return new BlockVar("ExtraBlock", IsUpgraded ? 5m : 4m, ValueProp.Move);
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var baseBlock = DynamicVars.Block.BaseValue;
        var extraPerEnemy = ((BlockVar)DynamicVars["ExtraBlock"]).BaseValue;
        const int pressurePerEnemy = 2;

        var enemyCount = CombatState?.HittableEnemies?.Count(e => e.IsAlive) ?? 0;

        var totalBlock = baseBlock + extraPerEnemy * enemyCount;
        var totalPressure = pressurePerEnemy * enemyCount;

        if (totalBlock > 0)
        {
            var blockVar = new BlockVar(totalBlock, ValueProp.Move);
            await CreatureCmd.GainBlock(Owner.Creature, blockVar, cardPlay);
        }

        if (totalPressure > 0)
            await PowerCmd.Apply<PressurePower>(Owner.Creature, totalPressure, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        ((BlockVar)DynamicVars["ExtraBlock"]).UpgradeValueBy(1m);
    }
}