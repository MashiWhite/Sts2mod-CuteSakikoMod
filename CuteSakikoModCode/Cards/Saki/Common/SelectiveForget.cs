using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class SelectiveForget() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 获取弃牌堆中的回忆卡牌
        var discardPile = PileType.Discard.GetPile(Owner);
        var memoryCards = discardPile.Cards
            .Where(card => card.HasModKeyword(CutesakiKeywords.Memory))
            .ToList();

        if (memoryCards.Count == 0) return;

        // 确定要遗忘的数量
        var count = IsUpgraded ? 2 : 1;
        count = Math.Min(count, memoryCards.Count);

        // 使用 UpFront 随机源，确保可重现
        var rng = Owner.RunState.Rng.UpFront;
        var cardsToForget = new List<CardModel>();
        for (var i = 0; i < count; i++)
        {
            var index = rng.NextInt(memoryCards.Count);
            cardsToForget.Add(memoryCards[index]);
            memoryCards.RemoveAt(index);
        }

        // 一次性遗忘所有选中的牌
        if (cardsToForget.Count > 0)
             MemoryCmd.Forget(choiceContext, cardsToForget, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}