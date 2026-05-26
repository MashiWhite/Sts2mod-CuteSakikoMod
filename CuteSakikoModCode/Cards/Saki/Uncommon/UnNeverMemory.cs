
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
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
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var x = ResolveEnergyXValue();
        if (x <= 0) return;

        var forgetPile = ForgetCardPile.Get(Owner);
        if (forgetPile == null) return;

        int count = x + (IsUpgraded ? 2 : 0);
        var rng = Owner.RunState.Rng.CombatCardSelection;

        for (int i = 0; i < count; i++)
        {
            // 每次循环重新从遗忘牌堆获取最新的记忆牌列表
            var memoryCards = forgetPile.Cards
                .Where(c => c.HasModKeyword(CutesakiKeywords.Memory))
                .ToList();

            if (memoryCards.Count == 0) break;

            var selected = rng.NextItem(memoryCards);
            if (selected == null) break;

            await CardCmd.AutoPlay(choiceContext, selected, null);
            await MemoryCmd.Forget(choiceContext, new[] { selected }, null);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级仅增加打出数量（X+2），逻辑已在 OnPlay 中处理
    }
}