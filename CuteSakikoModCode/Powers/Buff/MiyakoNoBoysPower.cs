using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     男子禁止：藤都子是百合党，讨厌百合破坏。
///     只挂在藤都子身上：按场上玩家性别，改她受到的伤害（男性加成）与她造成的伤害（女性减伤）。
///     单人 25% / 25% / 10%+10%；联机每人 7.5% / 7.5% / 4%+4%，且全队共享。
/// </summary>
public sealed class MiyakoNoBoysPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("DamageBonus", 0m);
            yield return new DynamicVar("DamageReduction", 0m);
        }
    }

    private bool IsSinglePlayer => (Owner?.CombatState?.Players.Count ?? 0) <= 1;

    private (decimal Bonus, decimal Reduction) Compute()
    {
        var state = Owner?.CombatState;
        if (state == null) return (0m, 0m);

        var masculine = 0;
        var feminine = 0;
        var neutral = 0;
        foreach (var player in state.Players)
        {
            switch (player.Character.Gender)
            {
                case CharacterGender.Masculine:
                    masculine++;
                    break;
                case CharacterGender.Feminine:
                    feminine++;
                    break;
                default:
                    neutral++;
                    break;
            }
        }

        if (IsSinglePlayer)
            return masculine > 0 ? (25m, 0m) : feminine > 0 ? (0m, 25m) : (10m, 10m);

        return (masculine * 7.5m + neutral * 4m, feminine * 7.5m + neutral * 4m);
    }

    private void RefreshVars()
    {
        var (bonus, reduction) = Compute();
        DynamicVars["DamageBonus"].BaseValue = bonus;
        DynamicVars["DamageReduction"].BaseValue = reduction;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        RefreshVars();
    }

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == Owner?.Side) RefreshVars();
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        var owner = Owner;
        if (owner == null || amount <= 0m) return 1m;

        var (bonus, reduction) = Compute();

        // 玩家打藤都子：男性/中性让伤害变高
        if (target == owner && dealer is { IsPlayer: true })
            return 1m + bonus / 100m;

        // 藤都子打玩家：女性/中性让伤害变低
        if (dealer == owner && target is { IsPlayer: true })
            return 1m - reduction / 100m;

        return 1m;
    }

    protected override string SmartDescriptionLocKey
    {
        get
        {
            var (bonus, reduction) = Compute();

            if (IsSinglePlayer)
            {
                if (bonus > 0 && reduction > 0) return Id.Entry + ".smartDescriptionSingleNeutral";
                if (bonus > 0) return Id.Entry + ".smartDescriptionSingleMale";
                return Id.Entry + ".smartDescriptionSingleFemale";
            }

            if (bonus > 0 && reduction > 0) return Id.Entry + ".smartDescription";
            if (bonus > 0) return Id.Entry + ".smartDescriptionBonus";
            return Id.Entry + ".smartDescriptionReduction";
        }
    }
}
