using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class BearMemory : CuteSakikoModCard
{
    public BearMemory() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 重放层数基础 
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Replay", 1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 显示一个静态的重放图标（无需动态数字，数字已在描述中体现）
            yield return HoverTipFactory.Static(StaticHoverTip.ReplayStatic);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 收集所有牌堆中的记忆牌（手牌、抽牌堆、弃牌堆、消耗堆）
        var memoryCards = new List<CardModel>();
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust })
        {
            var pile = pileType.GetPile(Owner);
            if (pile != null)
                memoryCards.AddRange(pile.Cards.Where(c =>
                    c.Keywords.Contains(CutesakiKeywords.Memory.GetModCardKeyword())));
        }

        if (memoryCards.Count == 0) return;

        // 随机选取一张（联机安全的随机）
        var randomCard = Owner.RunState.Rng.CombatCardSelection.NextItem(memoryCards);
        if (randomCard == null) return;

        // 增加重放次数
        var replayAmount = DynamicVars["Replay"].IntValue;
        randomCard.BaseReplayCount += replayAmount;

        // 视觉预览选中的牌
        CardCmd.Preview(randomCard);
    }

    protected override void OnUpgrade()
    {
        // 让描述中的重放层数从 1 变为 2
        DynamicVars["Replay"].UpgradeValueBy(1m);
    }
}