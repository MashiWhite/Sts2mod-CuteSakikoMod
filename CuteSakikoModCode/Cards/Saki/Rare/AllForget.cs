using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems.Memory;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class AllForget() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile == null) return;
        var handCards = handPile.Cards.ToList();
        if (handCards.Count == 0) return;

        int count = handCards.Count;

        // 遗忘所有手牌
        await MemoryCmd.Forget(choiceContext, handCards, this);

        // 每遗忘一张牌，获得一张回忆（升级后获得回忆+）
        if (count > 0)
        {
            await MemoryCmd.Recall(
                choiceContext,
                Owner,
                allowChoose: false,
                count: count,
                upgraded: IsUpgraded,
                source: this,
                allowDuplicates: true);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果在 OnPlay 中通过 IsUpgraded 处理（改为获得回忆+）
    }
}