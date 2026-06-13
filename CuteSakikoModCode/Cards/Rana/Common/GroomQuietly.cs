using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class GroomQuietly() : CuteRanaCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
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
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 将一张猫咪加入手牌
        var allNekoCards = ModelDb.CardPool<CuteSakikoTokenCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
            .ToList();

        if (allNekoCards.Count == 0) return;

        var combatState = Owner.Creature.CombatState!;
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var template = rng.NextItem(allNekoCards);
        var catCard = combatState.CreateCard(template, Owner);
        if (IsUpgraded)
        {
            catCard.UpgradeInternal();
            catCard.FinalizeUpgradeInternal();
        }

        await CardPileCmd.AddGeneratedCardToCombat(catCard, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m); // 8 → 10
    }
}