using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class StrikeOpulent() : CuteSakikoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Playpiano.GetModCardKeyword()];

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(4m, ValueProp.Move); // 基础伤害固定 4
            yield return new CalculatedIntVar("TotalExtraHits", (card, target) =>
            {
                var allCards = card?.Owner?.PlayerCombatState?.AllCards;
                if (allCards == null) return 0;
                var qinCount = allCards.Count(c => c.Keywords.Contains(CutesakiKeywords.Playpiano.GetModCardKeyword()));
                var multiplier = card!.IsUpgraded ? 2 : 1;
                return qinCount * multiplier;
            });
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var damage = DynamicVars.Damage.BaseValue;
        var qinCount =
            Owner.PlayerCombatState.AllCards.Count(c =>
                c.Keywords.Contains(CutesakiKeywords.Playpiano.GetModCardKeyword()));
        var multiplier = IsUpgraded ? 2 : 1;
        var totalExtraHits = qinCount * multiplier;
        var totalHits = 1 + totalExtraHits; // 基础 1 次 + 额外次数

        // 所有命中合并在一个 AttackCommand 里，活力等 buff 全部生效
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .WithHitCount(totalHits)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级效果已通过 multiplier 体现，无需额外操作
    }
}

// 自定义动态变量：整数类型，用于描述
public class CalculatedIntVar : DynamicVar
{
    private readonly Func<CardModel, Creature?, int> _calculator;

    public CalculatedIntVar(string name, Func<CardModel, Creature?, int> calculator) : base(name, 0)
    {
        _calculator = calculator;
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
        bool runGlobalHooks)
    {
        BaseValue = _calculator(card, target);
    }
}