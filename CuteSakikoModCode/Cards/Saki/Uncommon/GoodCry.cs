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

public class GoodCry : CuteSakikoModCard
{
    public GoodCry() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(IsUpgraded ? 2 : 1); }
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
        var creature = Owner.Creature;

        // 减少5层压力
        var pressure = creature.GetPower<PressurePower>();
        if (pressure != null && pressure.Amount > 0)
        {
            var reducePressure = 5;
            await PowerCmd.ModifyAmount(pressure, -reducePressure, creature, this);
        }

        // 抽牌
        var drawCount = IsUpgraded ? 2 : 1;
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);

        // 若有崩溃，额外减少崩溃层数
        var breakdown = creature.GetPower<BreakDownPower>();
        if (breakdown != null && breakdown.Amount > 0)
        {
            var reduceBreakdown = IsUpgraded ? 2 : 1;
            await PowerCmd.ModifyAmount(breakdown, -reduceBreakdown, creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果在 CanonicalVars 中处理抽牌数，崩溃减少在逻辑中判断
    }
}