
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class PressureDmgPower : CuteSakikoModPower
{
    private int _strengthGained;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var pressure = Owner.GetPower<PressurePower>();
        var pressureAmount = pressure?.Amount ?? 0;

        var strengthToGain = pressureAmount / 5 * Amount; // 每5层压力获得 Amount 力量
        if (strengthToGain > 0)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext,Owner, strengthToGain, Owner, null);
            _strengthGained = strengthToGain;
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        if (_strengthGained > 0)
        {
            var strength = Owner.GetPower<StrengthPower>();
            if (strength != null)
                // 使用 PowerCmd.ModifyAmount 安全减少力量层数
                await PowerCmd.ModifyAmount(choiceContext,strength, -_strengthGained, Owner, null);
            _strengthGained = 0;
        }
    }
}