
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class Huh() : CuteAnonCard(2, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
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
        var shuffleRng = Owner.RunState.Rng.Shuffle; // 用于 UnstableShuffle 的随机源

        // 攻击循环：不消耗 CombatCardSelection
        var hittable = combat.HittableEnemies.ToList();
        for (var i = 0; i < _hitCount; i++)
        {
            if (hittable.Count == 0) break;
            var target = hittable.UnstableShuffle(shuffleRng).First();
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        // 额外获得一个随机音符（不消耗 CombatCardSelection）
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
        {
            var noteTypes = new[] { CardType.Attack, CardType.Skill, CardType.Power };
            // 使用洗牌随机源生成索引（不消耗 CombatCardSelection）
            int index = shuffleRng.NextInt(noteTypes.Length);
            var randomType = noteTypes[index];

            var mainChords = guitar.GetCurrentChords();
            var bonusChords = guitar.GetBonusChords();
            var tempChords = guitar.GetTemporaryChords();

            MusicNoteManager.AddNote(Owner, randomType, mainChords,
                bonusChords.Concat(tempChords));

            guitar.UpdateNoteDisplay();
            guitar.UpdateStoredChordDisplay();
        }
    }

    protected override void OnUpgrade()
    {
        _hitCount = 5;
    }
}