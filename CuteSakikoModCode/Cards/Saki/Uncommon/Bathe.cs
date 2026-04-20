using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;


public class Bathe : CuteSakikoModCard
{
    public Bathe() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }
    

    // 不再使用 Exhaust 关键词，因为结果堆由 GetResultPileType 动态决定
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new CardsVar(IsUpgraded ? 2 : 1);
            yield return new EnergyVar(IsUpgraded ? 2 : 1);
        }
    }

    // 动态决定卡牌打出后的去向
    protected override PileType GetResultPileType()
    {
        var pressure = Owner.Creature.GetPower<PressurePower>();
        var required = IsUpgraded ? 10 : 15;
        var enough = pressure != null && pressure.Amount >= required;
        return enough ? PileType.Hand : PileType.Exhaust;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先执行抽牌和获得能量
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);

        // 检查并消耗压力（如果压力足够）
        var pressure = Owner.Creature.GetPower<PressurePower>();
        var required = IsUpgraded ? 10 : 15;
        if (pressure != null && pressure.Amount >= required)
            await PowerCmd.ModifyAmount(pressure, -required, Owner.Creature, this);
        // 卡牌的去向已经由 GetResultPileType 决定，无需额外操作
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在 CanonicalVars 中处理
    }
}