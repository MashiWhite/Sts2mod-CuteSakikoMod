
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class MillionTimesCat : CuteSakikoEventRelic
{
    private const decimal ReviveHealPercent = 0.3m; // 阻止死亡后恢复最大生命值的30%

    private bool _wasUsed = false;
    private decimal _healthAtStart;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 战后恢复量（固定3点）
            yield return new HealVar(3m);
        }
    }

    [SavedProperty]
    public bool WasUsed
    {
        get => _wasUsed;
        set
        {
            _wasUsed = value;
            if (_wasUsed)
                Status = RelicStatus.Disabled;
        }
    }

    // ----- 阻止死亡（参考 LizardTail）-----
    public override bool ShouldDieLate(Creature creature)
    {
        return creature != Owner?.Creature || WasUsed;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        WasUsed = true;
        // 阻止死亡后恢复 30% 最大生命值
        decimal healAmount = creature.MaxHp * ReviveHealPercent;
        await CreatureCmd.Heal(creature, healAmount);
    }

    // ----- 战斗中受伤后恢复（固定3点）-----
    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        if (Owner != null)
            _healthAtStart = Owner.Creature.CurrentHp;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);

        if (!WasUsed && Owner != null && !Owner.Creature.IsDead)
        {
            // 如果当前生命值小于战斗开始时的生命值，说明受过伤
            if (Owner.Creature.CurrentHp < _healthAtStart)
            {
                Flash();
                // 战后恢复固定 3 点（由 HealVar 控制）
                await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
            }
        }
    }
}