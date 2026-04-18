using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
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

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class StrikeOpulent : CustomCardModel
{
    public StrikeOpulent() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Playpiano];

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(IsUpgraded ? 8m : 6m, ValueProp.Move);
            yield return new DamageVar("ExtraDamage", 2m, ValueProp.Move);
            // 添加一个动态变量，显示额外攻击次数
            yield return new CalculatedIntVar("TotalExtraHits", (card, target) =>
            {
                var owner = card.Owner;
                if (owner == null) return 0;
                var qinCount =
                    owner.PlayerCombatState.AllCards.Count(c => c.Keywords.Contains(CutesakiKeywords.Playpiano));
                var multiplier = card.IsUpgraded ? 2 : 1;
                return qinCount * multiplier;
            });
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
        if (cardPlay.Target == null) return;

        // 基础伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 计算额外攻击次数
        var qinCount = Owner.PlayerCombatState.AllCards.Count(c => c.Keywords.Contains(CutesakiKeywords.Playpiano));
        var multiplier = IsUpgraded ? 2 : 1;
        var totalExtraHits = qinCount * multiplier;
        decimal extraDamage = ((DamageVar)DynamicVars["ExtraDamage"]).BaseValue ;
        
        for (var i = 0; i < totalExtraHits; i++)
            await DamageCmd.Attack(extraDamage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在 CanonicalVars 中已处理
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