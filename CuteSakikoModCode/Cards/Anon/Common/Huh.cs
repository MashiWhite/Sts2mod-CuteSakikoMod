using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class Huh() : CuteAnonCard(2, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
    // 攻击次数，升级后由4变为5
    private int _hitCount = 4;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(5m, ValueProp.Move); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        var damage = DynamicVars.Damage.BaseValue;
        var rng = Owner.RunState.Rng.CombatCardSelection;

        for (var i = 0; i < _hitCount; i++)
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

        // 额外获得一个随机音符（排除Status）
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
        {
            var noteTypes = new[] { CardType.Attack, CardType.Skill, CardType.Power };
            var randomType = noteTypes[rng.NextInt(noteTypes.Length)];

            // 使用真实和弦列表
            var mainChords = guitar.GetCurrentChords();
            var bonusChords = guitar.GetBonusChords();
            var tempChords = guitar.GetTemporaryChords();

            MusicNoteManager.AddNote(Owner, randomType, mainChords,
                bonusChords.Concat(tempChords));

            // 刷新 UI
            guitar.UpdateNoteDisplay();
            guitar.UpdateStoredChordDisplay();
        }
    }

    protected override void OnUpgrade()
    {
        _hitCount = 5; // 攻击次数提升为5
    }
}