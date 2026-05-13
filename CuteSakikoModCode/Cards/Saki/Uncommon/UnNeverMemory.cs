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

        var memoryCards = forgetPile.Cards
            .Where(c => c.HasModKeyword(CutesakiKeywords.Memory))
            .ToList();

        if (memoryCards.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var played = 0;
        while (played < x && memoryCards.Count > 0)
        {
            var idx = rng.NextInt(memoryCards.Count);
            var selected = memoryCards[idx];
            memoryCards.RemoveAt(idx);

            await CardCmd.AutoPlay(choiceContext, selected, null);

            if (IsUpgraded)
                 MemoryCmd.Forget(choiceContext, new[] { selected }, null);

            played++;
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在逻辑中处理
    }
}