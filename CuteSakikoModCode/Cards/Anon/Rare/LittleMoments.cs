using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class LittleMoments() : CuteAnonCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    // 改为实例字段，避免多个玩家相互影响
    private bool _isTransforming;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<Lifetime>(IsUpgraded); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(2); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        if (CombatState != null && Owner != null)
        {
            var copy = CombatState.CreateCard<LittleMoments>(Owner);
            if (IsUpgraded)
            {
                copy.UpgradeInternal();
                copy.FinalizeUpgradeInternal();
            }

            await CardPileCmd.Add(copy, PileType.Discard, CardPilePosition.Top);
        }

        await Cmd.CustomScaledWait(0.1f, 0.15f);

        await TryTransform();
    }

    private async Task TryTransform()
    {
        if (_isTransforming) return;
        if (CombatState == null || Owner == null) return;

        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null) return;

        var handCount = hand.Cards.OfType<LittleMoments>().Count();
        if (handCount < 5) return;

        _isTransforming = true;

        // 收集所有牌堆中的 LittleMoments
        var targetPiles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
        var allLittleMoments = targetPiles
            .Select(pileType => pileType.GetPile(Owner))
            .Where(pile => pile != null)
            .SelectMany(pile => pile.Cards.OfType<LittleMoments>())
            .ToList();

        // 移除全部
        foreach (var card in allLittleMoments)
            await CardPileCmd.RemoveFromCombat(card);

        // 创建 Lifetime
        var lifetime = CombatState.CreateCard<Lifetime>(Owner);
        if (IsUpgraded)
        {
            lifetime.UpgradeInternal();
            lifetime.FinalizeUpgradeInternal();
        }
        await CardPileCmd.AddGeneratedCardToCombat(lifetime, PileType.Hand, Owner);

        _isTransforming = false;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}