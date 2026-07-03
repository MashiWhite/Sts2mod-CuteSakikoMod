using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class BecomeAshesPower : CuteSakikoModPower
{
    private bool _hasGrantedAiHeart;
    private int _initialMaxHp; // 记录获得此能力时的最大生命值，用于固定二阶段阈值

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("SelfDamage", 0);
            yield return new DynamicVar("StrengthGain", 0);
            yield return new DynamicVar("PhaseTwoThreshold", 0); // 二阶段血量阈值
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        // 记录此时的真实最大生命值作为初始值
        _initialMaxHp = Owner.MaxHp;
        UpdatePhaseTwoThreshold();
        UpdateSelfDamage();
        UpdateStrengthGain();
    }

    private void UpdatePhaseTwoThreshold()
    {
        if (Owner != null)
            DynamicVars["PhaseTwoThreshold"].BaseValue = (int)(_initialMaxHp * 0.85);
    }

    public override Task AfterCurrentHpChanged(Creature creature, Decimal delta)
    {
        if (creature == Owner)
            UpdateStrengthGain();
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner)) return;

        // 减少最大生命值（当前最大生命值的 5%），而不是造成伤害
        int maxHpLoss = (int)Math.Ceiling(Owner.MaxHp * 0.05);
        if (maxHpLoss > 0)
            await CreatureCmd.LoseMaxHp(choiceContext, Owner, maxHpLoss, false);

        UpdateStrengthGain();
        UpdateSelfDamage(); // 最大生命值改变后，下次损失的值也会变化
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side) return;

        float lostPercent = (float)(_initialMaxHp - Owner.CurrentHp) / _initialMaxHp * 100f;
        int strength = Math.Max(1, (int)Math.Floor(lostPercent / 20f));

        // 二阶段条件：当前生命值低于初始最大生命值的 85%
        if (Owner.CurrentHp < _initialMaxHp * 0.85)
        {
            strength *= 2;
            if (!_hasGrantedAiHeart)
            {
                await PowerCmd.Apply<AiHeartPower>(
                    new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
                _hasGrantedAiHeart = true;
            }
        }

        if (strength > 0)
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(), Owner, strength, Owner, null);

        UpdateStrengthGain();
    }

    private void UpdateSelfDamage()
    {
        if (Owner != null)
            DynamicVars["SelfDamage"].BaseValue = (int)Math.Ceiling(Owner.MaxHp * 0.05);
    }

    private void UpdateStrengthGain()
    {
        if (Owner != null)
        {
            float lostPercent = (float)(_initialMaxHp - Owner.CurrentHp) / _initialMaxHp * 100f;
            int strength = Math.Max(2, (int)Math.Floor(lostPercent / 20f));
            if (Owner.CurrentHp < _initialMaxHp * 0.85)
                strength *= 2;
            DynamicVars["StrengthGain"].BaseValue = strength;
        }
    }
}