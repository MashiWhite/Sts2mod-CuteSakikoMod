using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

[Pool(typeof(CuteSakiCardPool))]
public class SelectiveForget() : CustomCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 获取弃牌堆中的回忆卡牌
        var discardPile = PileType.Discard.GetPile(Owner);

        var memoryCards = discardPile.Cards
            .Where(card => card.CanonicalKeywords.Contains(CutesakiKeywords.Memory))
            .ToList();

        if (memoryCards.Count == 0) return;

        // 确定要消耗的数量
        var count = IsUpgraded ? 2 : 1;
        count = Math.Min(count, memoryCards.Count);

        // 使用 UpFront 随机源，确保可重现
        var rng = Owner.RunState.Rng.UpFront;
        for (var i = 0; i < count; i++)
        {
            var index = rng.NextInt(memoryCards.Count);
            var cardToExhaust = memoryCards[index];
            await CardCmd.Exhaust(choiceContext, cardToExhaust);
            memoryCards.RemoveAt(index);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}