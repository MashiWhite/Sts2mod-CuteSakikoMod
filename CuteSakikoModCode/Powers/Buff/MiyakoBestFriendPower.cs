using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     最好的朋友：丸君（手偶）在场时，藤都子每回合开始回复 5% 最大生命；
///     丸君被打破后，藤都子每回合开始获得 3 点力量。
///     显示只保留当前生效的那一段。
/// </summary>
public sealed class MiyakoBestFriendPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("HealPercent", 5m);
            yield return new DynamicVar("StrengthGain", 3m);
        }
    }

    private bool MaruAlive =>
        Owner?.CombatState?.Enemies.Any(e => e.IsAlive && e.Monster is MaruKun) ?? false;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        var owner = Owner;
        if (owner == null || side != owner.Side || !owner.IsAlive) return;
        if (!MaruAlive) return;

        var heal = (int)Math.Ceiling(owner.MaxHp * DynamicVars["HealPercent"].BaseValue / 100m);
        if (heal > 0)
            await CreatureCmd.Heal(owner, heal);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        var owner = Owner;
        if (owner == null || side != owner.Side || !owner.IsAlive) return;
        if (MaruAlive) return;

        var strength = (int)DynamicVars["StrengthGain"].BaseValue;
        if (strength > 0)
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(), owner, strength, owner, null);
    }

    protected override string SmartDescriptionLocKey =>
        Id.Entry + (MaruAlive ? ".smartDescriptionMaruAlive" : ".smartDescriptionMaruDead");
}
