using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class WaitItCool : CuteRanaCard
{
    [SavedProperty]
    private int TimesRetainedThisCombat { get; set; }

    public WaitItCool() : base(3, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Retain;
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new EnergyVar(2);
            yield return new CardsVar(2);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int energy = DynamicVars.Energy.IntValue;
        int draw = DynamicVars.Cards.IntValue;

        if (energy > 0)
            await PlayerCmd.GainEnergy(energy, Owner);
        if (draw > 0)
            await CardPileCmd.Draw(choiceContext, draw, Owner);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy) return;
        if (Pile?.Type != PileType.Hand) return;
        if (IsExhausted) return;

        TimesRetainedThisCombat++;

        // 获取基础费用（升级后的费用，不含任何本地或全局修饰）
        int baseCost = EnergyCost.GetWithModifiers((CostModifiers)0);
        int newCost = Math.Max(0, baseCost - TimesRetainedThisCombat);

        // 设置为新费用，并确保只降低不升高（reduceOnly = true）
        EnergyCost.SetThisCombat(newCost, true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 3c -> 2c
    }

    private bool IsExhausted => Pile == null;
}