using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class OblivionisPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    static OblivionisPower()
    {
        MemoryCardPileManager.CardsForgotten += OnCardsForgotten;
    }

    private static async Task OnCardsForgotten(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> forgottenCards,
        CardModel? source)
    {
        if (forgottenCards == null || forgottenCards.Count == 0)
            return;

        var owner = forgottenCards[0].Owner;
        if (owner == null)
            return;

        var power = owner.Creature?.GetPower<OblivionisPower>();
        if (power == null || power.Amount <= 0)
            return;

        var combatState = owner.Creature.CombatState;
        if (combatState == null)
            return;

        int damagePerCard = power.Amount;

        for (int i = 0; i < forgottenCards.Count; i++)
        {
            var enemies = combatState.HittableEnemies;
            if (enemies.Count == 0)
                break;

            await CreatureCmd.Damage(
                choiceContext,
                enemies,
                new DamageVar(damagePerCard, ValueProp.Unpowered),
                owner.Creature,
                (CardModel?)null,
                (CardPlay?)null
            );
        }
    }
}