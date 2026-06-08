using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class BecomeAshesPower : CuteSakikoModPower
{
    private bool _hasGrantedAiHeart;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("SelfDamage", 0);
            yield return new DynamicVar("StrengthGain", 0);
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        UpdateSelfDamage();
        UpdateStrengthGain();
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

        int selfDamage = (int)Math.Ceiling(Owner.MaxHp * 0.05);
        if (selfDamage > 0)
            await CreatureCmd.Damage(choiceContext, Owner, selfDamage,
                ValueProp.Unblockable | ValueProp.Unpowered, null, null);

        UpdateStrengthGain();
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side) return;

        float lostPercent = (float)(Owner.MaxHp - Owner.CurrentHp) / Owner.MaxHp * 100f;
        int strength = Math.Max(2, (int)Math.Floor(lostPercent / 15f));

        if (Owner.CurrentHp < Owner.MaxHp * 0.5)
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
            float lostPercent = (float)(Owner.MaxHp - Owner.CurrentHp) / Owner.MaxHp * 100f;
            int strength = Math.Max(2, (int)Math.Floor(lostPercent / 20f));
            if (Owner.CurrentHp < Owner.MaxHp * 0.5)
                strength *= 2;
            DynamicVars["StrengthGain"].BaseValue = strength;
        }
    }
}