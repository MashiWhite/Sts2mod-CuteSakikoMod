using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     乐章未尽（恐惧 dominion 版）：挂在本幕小怪身上。
///     10 层起算，每个敌方回合结束 -1，并按剩余层数获得效果；
///     悬停时实时显示当前天数对应的那一条（与本体 MovementNotFinishedPower 同一做法）。
/// </summary>
public sealed class TenDayDreadPower : CuteSakikoModPower
{
    private const int HealPercent = 10;
    private const int BlockAmount = 8;

        // 与本体「乐章未尽」同图标，玩家看到的是同一个 power
    public override PowerAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/images/powers/MovementNotFinishedPower.png",
        "res://CuteSakikoMod/images/powers/MovementNotFinishedPower.png");

    /// <summary>按当前剩余天数实时切到 effect.N 词条（与本体「乐章未尽」一致）。</summary>
    protected override string SmartDescriptionLocKey
    {
        get
        {
            if (!IsMutable)
                return base.SmartDescriptionLocKey;

            var day = (int)Amount;
            var key = $"{Id.Entry}.effect.{day}";
            return LocString.Exists("powers", key) ? key : base.SmartDescriptionLocKey;
        }
    }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy) return;
        if (Owner == null || !Owner.IsAlive) return;
        if (!participants.Contains(Owner)) return;

        await PowerCmd.Decrement(this);

        var day = (int)Amount;
        if (day <= 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        var ctx = new ThrowingPlayerChoiceContext();
        switch (day)
        {
            case 9:
                await PowerCmd.Apply<StrengthPower>(ctx, Owner, 2m, Owner, null);
                break;
            case 8:
                await HealOwner();
                break;
            case 7:
                await PowerCmd.Apply<VigorPower>(ctx, Owner, 2m, Owner, null);
                break;
            case 6:
                await CreatureCmd.GainBlock(Owner, BlockAmount, ValueProp.Move, null);
                break;
            case 5:
                await PowerCmd.Apply<DexterityPower>(ctx, Owner, 1m, Owner, null);
                break;
            case 4:
                await CreatureCmd.GainBlock(Owner, BlockAmount, ValueProp.Move, null);
                break;
            case 3:
                await HealOwner();
                break;
            case 2:
                await CreatureCmd.GainBlock(Owner, BlockAmount, ValueProp.Move, null);
                break;
            case 1:
                await HealOwner();
                break;
        }
    }

    private Task HealOwner()
    {
        var heal = Math.Max(1, (int)Math.Ceiling(Owner.MaxHp * HealPercent / 100m));
        return CreatureCmd.Heal(Owner, heal);
    }
}
