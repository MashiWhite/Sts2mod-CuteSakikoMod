using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class UnforgettablePerformance : CuteAnonCard
{
    public UnforgettablePerformance() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<UnforgettablePerformancePower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 描述中的阈值变量（升级后 3→2）
            yield return new DynamicVar("Threshold", 3m);
            yield return new EnergyVar(1);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var existing = Owner.Creature.GetPower<UnforgettablePerformancePower>();
        if (existing != null)
        {
            // 如果能力已存在，更新阈值并叠加层数
            existing.UpdateThreshold(IsUpgraded ? 2 : 3);
            await PowerCmd.ModifyAmount(choiceContext, existing, 1, Owner.Creature, this);
        }
        else
        {
            // 首次施加能力（AfterApplied 中会自动设置阈值）
            await PowerCmd.Apply<UnforgettablePerformancePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
        // 描述中阈值显示更新
        DynamicVars["Threshold"].BaseValue = 2m;
    }
}