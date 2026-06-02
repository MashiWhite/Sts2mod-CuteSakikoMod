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
    [SavedProperty] private int _timesRetainedThisCombat; // 本场战斗中已保留的次数

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

    // 每回合结束（敌方回合结束时即玩家回合结束）检查是否在手牌中
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy) return; // 只在玩家回合结束时处理
        if (Pile?.Type != PileType.Hand) return; // 不在手牌则无事
        if (IsExhausted) return; // 已经消耗掉的不处理（实际上消耗后牌就不在手牌了）

        // 增加保留计数，每次减少1费
        _timesRetainedThisCombat++;
        EnergyCost.AddThisCombat(-1);
        // 可选：如果费用降至0以下，可以重置为0（但AddThisCombat内部会处理）
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 3c -> 2c
    }

    // 辅助属性：判断是否已经消耗（Exhaust后牌不在任何Pile，或Pile.Type为None）
    private bool IsExhausted => Pile == null;
}