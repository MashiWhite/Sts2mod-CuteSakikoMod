using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;

public class NotNeeded() : ModStatusCard(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override bool HasTurnEndInHandEffect => true;  // 启用官方回合结束在手牌钩子

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
        get { yield return new BlockVar(2m, ValueProp.Move); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Task.CompletedTask;
    }

    // 回合结束在手牌时触发
    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        var blockAmount = DynamicVars.Block.IntValue;
        // 获得格挡（使用 decimal 重载，参数顺序：Creature, decimal, ValueProp, CardPlay?, bool）
        if (blockAmount > 0)
            await CreatureCmd.GainBlock(Owner.Creature, (decimal)blockAmount, ValueProp.Move, null, false);

        // 创建自身复制并加入手牌（保留一回合）
        var copy = CreateClone();
        copy.GiveSingleTurnRetain();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);

        // 自身获得保留
        GiveSingleTurnRetain();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m); // 2 → 4
    }
}