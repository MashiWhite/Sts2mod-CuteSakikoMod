using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Common;

public class Bonfire : CuteSakikoModEggCard
{
    public Bonfire() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust ];

    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner.Creature;
        var percent = IsUpgraded ? 0.1f : 0.05f;
        var healAmount = (int)(creature.MaxHp * percent);
        if (healAmount < 1) healAmount = 1;
        await CreatureCmd.Heal(creature, healAmount);

        var upgradeCount = IsUpgraded ? 2 : 1;
        var deck = PileType.Deck.GetPile(Owner).Cards;
        var upgradable = deck.Where(c => c.IsUpgradable).ToList();

        for (var i = 0; i < upgradeCount && upgradable.Count > 0; i++)
        {
            var randomCard = Owner.RunState.Rng.UpFront.NextItem(upgradable);

            // 升级牌库中的卡牌
            randomCard.UpgradeInternal();
            randomCard.FinalizeUpgradeInternal();

            // 同步升级当前战斗中所有以此卡为原版的副本
            var combatCards = Owner.PlayerCombatState?.AllCards;
            if (combatCards != null)
                foreach (var combatCard in combatCards.Where(c =>
                             c.Id == randomCard.Id && c.DeckVersion == randomCard && c.IsUpgradable))
                {
                    combatCard.UpgradeInternal();
                    combatCard.FinalizeUpgradeInternal();
                }

            upgradable.Remove(randomCard);
        }
    }

    protected override void OnUpgrade()
    {
        // 效果由 OnPlay 中的 IsUpgraded 控制
    }
}