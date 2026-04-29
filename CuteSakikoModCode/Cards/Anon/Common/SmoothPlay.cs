using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class SmoothPlay() : CuteAnonCard(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(3m, ValueProp.Move); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        // 统计攻击音符数量
        var currentNotes = MusicNoteManager.GetCurrentNotes(Owner);
        var attackCount = currentNotes.Count(n => n == CardType.Attack);

        // 清除所有音符
        MusicNoteManager.ClearNotes(Owner);

        // 根据攻击音符数量造成多次随机伤害
        var damage = DynamicVars.Damage.IntValue;
        var rng = Owner.RunState.Rng.CombatCardSelection;
        for (var i = 0; i < attackCount; i++)
        {
            var hittable = combat.HittableEnemies;
            if (!hittable.Any()) break;
            var target = rng.NextItem(hittable);
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        // 演奏最新储存的和弦
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
            await guitar.TriggerLastStoredChord(choiceContext);

        // 刷新音符UI
        guitar?.UpdateNoteDisplay();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 3 → 5
    }
}