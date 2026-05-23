using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        // 打出数量：基础为 X，升级后 +2
        int count = x + (IsUpgraded ? 2 : 0);

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var played = 0;
        while (played < count && memoryCards.Count > 0)
        {
            var idx = rng.NextInt(memoryCards.Count);
            var selected = memoryCards[idx];
            memoryCards.RemoveAt(idx);
            await CardCmd.AutoPlay(choiceContext, selected, null);

            // 初始自带遗忘，每张自动打出的牌都会被遗忘
            MemoryCmd.Forget(choiceContext, new[] { selected }, null);

            played++;
        }
    }

    protected override void OnUpgrade()
    {
        // 升级仅增加打出数量，逻辑已在 OnPlay 中处理
    }
}