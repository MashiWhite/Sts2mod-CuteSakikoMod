
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MeteorHephaestusPower : CuteSakikoModPower
{
    private bool _isUpgraded; // 决定是否翻倍层数

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠层，层数决定夺取次数

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new BoolVar("IsUpgraded", _isUpgraded);
            yield return new IntVar("TargetCount", Amount);
        }
    }

    public void SetUpgraded(bool upgraded)
    {
        _isUpgraded = upgraded;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        if (Owner.CombatState == null) return;
        if (Amount <= 0) return;

        // 获取所有队友（同侧且不是自己）
        var teammates = Owner.CombatState.Creatures.Where(c => c.Side == Owner.Side && c != Owner && c.IsAlive)
            .ToList();
        if (teammates.Count == 0) return;

        var attempts = Amount; // 尝试次数等于层数
        for (var i = 0; i < attempts; i++)
        {
            // 随机选择一个队友
            if (teammates.Count == 0) break;
            var target = Owner.CombatState.RunState.Rng.CombatCardSelection.NextItem(teammates);
            if (target == null) continue;

            // 获取目标身上的所有 Buff Power
            var buffs = target.Powers.Where(p => p.Type == PowerType.Buff && p != this).ToList();
            if (buffs.Count == 0) continue;

            // 随机选择一个 Buff
            var selectedBuff = Owner.CombatState.RunState.Rng.CombatCardSelection.NextItem(buffs);
            if (selectedBuff == null) continue;

            // 记录信息
            var powerId = selectedBuff.Id;
            var amount = selectedBuff.Amount;
            var stackType = selectedBuff.StackType;

            // 移除该 Buff
            await PowerCmd.Remove(selectedBuff);

            // 添加到自己身上
            var finalAmount = amount;
            if (_isUpgraded && stackType == PowerStackType.Counter && amount > 0) finalAmount *= 2;
            var canonical = ModelDb.GetById<PowerModel>(powerId);
            if (canonical != null)
            {
                var newPower = canonical.ToMutable();
                await PowerCmd.Apply(choiceContext,newPower, Owner, finalAmount, Owner, null);
            }
        }
    }
}