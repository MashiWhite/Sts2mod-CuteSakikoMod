using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class UnNeverMemory() : CuteSakikoModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var x = ResolveEnergyXValue();
        if (x <= 0) return;

        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        if (exhaustPile == null) return;

        // 获取消耗堆中的所有回忆牌（可变实例）并复制一份列表，避免迭代中修改
        var memoryCards = exhaustPile.Cards
            .Where(c => c.HasModKeyword(CutesakiKeywords.Memory))
            .ToList();

        if (memoryCards.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var played = 0;
        while (played < x && memoryCards.Count > 0)
        {
            var idx = rng.NextInt(memoryCards.Count);
            var selected = memoryCards[idx];
            // 从列表中移除已选中的，避免重复选择同一张（如果希望允许重复，则不移除）
            memoryCards.RemoveAt(idx);
            // 直接打出该牌（不需要移除，AutoPlay会自动处理牌堆移动）
            await CardCmd.AutoPlay(choiceContext, selected, null);
            // 如果升级，确保牌被消耗（如果打出后不在消耗堆，则移入消耗堆）
            if (IsUpgraded && selected.Pile?.Type != PileType.Exhaust) await CardCmd.Exhaust(choiceContext, selected);
            played++;
            // 注意：由于 selected 可能已经被移出消耗堆，所以 memoryCards 列表中的元素可能已经无效，
            // 但我们已经移除了该元素，所以没问题。但打出后可能其他牌的状态不变，继续使用剩余列表即可。
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果在逻辑中已处理
    }
}