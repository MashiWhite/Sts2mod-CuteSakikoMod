using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class BiteGuitar : CuteAnonCard
{
    public BiteGuitar() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DamageVar(15m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        TriggerBanter();

        // 伤害
        var damage = DynamicVars.Damage.IntValue;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 随机化已记忆和弦（独立随机每个和弦的音符，带权重）
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        var rng = combat.RunState.Rng.CombatCardSelection;
        var weightedPool = new (CardType type, double weight)[]
        {
            (CardType.Attack, 0.48),
            (CardType.Skill, 0.48),
            (CardType.Power, 0.04)
        };

        foreach (var chordId in guitar.GetLearnedChordIds())
        {
            if (!ChordManager.AllChords.TryGetValue(chordId, out var def)) continue;

            // 长度不变，每个位置按权重独立随机选取类型
            var shuffled = def.NoteSequence.Select(_ => PickRandomWeighted(rng, weightedPool)).ToList();
            ChordSequenceModifierHelper.SetCardModifier(Owner, chordId, new ShuffleNotesModifier(shuffled));
        }

        // 刷新吉他 UI
        guitar.UpdateStoredChordDisplay();
        guitar.UpdateNoteDisplay();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m); // 15 → 20
    }

    /// <summary> 根据权重随机选取一个 CardType </summary>
    private static CardType PickRandomWeighted(Rng rng, (CardType type, double weight)[] pool)
    {
        double totalWeight = pool.Sum(w => w.weight);
        double roll = rng.NextFloat() * totalWeight;
        double cumulative = 0;
        foreach (var item in pool)
        {
            cumulative += item.weight;
            if (roll <= cumulative) return item.type;
        }
        return pool.Last().type;
    }
}