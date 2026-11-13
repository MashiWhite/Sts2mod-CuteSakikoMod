using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     男子禁止（弱）：恐惧模式 dominion 版本，挂在本幕的小怪身上。
///     与藤都子本体的区别只有数值：单人 15%/15%，多人每人 5%/5%（中性 5%/5%）。
/// </summary>
public sealed class WeakenedMiyakoNoBoysPower : CuteSakikoModPower
{
        // 复用本体「男子禁止」的图标（新贴图未进 Godot 导入缓存）
    public override PowerAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/images/powers/MiyakoNoBoysPower.png",
        "res://CuteSakikoMod/images/powers/MiyakoNoBoysPower.png");

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
            return masculine > 0 ? (15m, 0m) : feminine > 0 ? (0m, 15m) : (15m, 15m);

        return (masculine * 5m + neutral * 5m, feminine * 5m + neutral * 5m);
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

        // 玩家打小怪：女性/中性让它受到的伤害变低（减伤）
        if (target == owner && dealer is { IsPlayer: true })
            return 1m - reduction / 100m;

        // 小怪打玩家：男性/中性让它造成的伤害变高（伤害加成）
        if (dealer == owner && target is { IsPlayer: true })
            return 1m + bonus / 100m;

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
