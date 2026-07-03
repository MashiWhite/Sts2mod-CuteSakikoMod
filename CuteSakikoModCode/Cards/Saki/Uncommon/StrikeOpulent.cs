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

public class StrikeOpulent : CuteSakikoModCard
{
    public StrikeOpulent() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Playpiano.GetModCardKeyword()];

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(4m, ValueProp.Move); // 基础伤害 4（升级后 +2 → 6）
            yield return new CalculatedIntVar("TotalExtraHits", (card, target) =>
            {
                var allCards = card?.Owner?.PlayerCombatState?.AllCards;
                if (allCards == null) return 0;
                return allCards.Count(c => c.Keywords.Contains(CutesakiKeywords.Playpiano.GetModCardKeyword()));
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
        var totalExtraHits = DynamicVars["TotalExtraHits"].IntValue;
        var totalHits = 1 + totalExtraHits; // 基础 1 次 + 额外次数

        await DamageCmd.Attack(damage)
            .FromCard(this,cardPlay)
            .WithHitCount(totalHits)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 4 → 6
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