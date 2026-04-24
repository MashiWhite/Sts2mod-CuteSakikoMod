
using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common
{
    public class Huh() : CuteAnonCard(2, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(5m, ValueProp.Move);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var combat = Owner.Creature.CombatState;
            if (combat == null) return;

            int hitCount = IsUpgraded ? 5 : 4;
            var damage = DynamicVars.Damage.BaseValue;
            var rng = Owner.RunState.Rng.CombatCardSelection;

            // 进行多次随机攻击
            for (int i = 0; i < hitCount; i++)
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

            // 额外获得一个随机音符（不能是Status）
            var noteTypes = new[] { CardType.Attack, CardType.Skill, CardType.Power };
            CardType randomType = noteTypes[rng.NextInt(noteTypes.Length)];
            MusicNoteManager.AddNote(Owner, randomType,
                new Dictionary<ChordCategory, string>(),
                Enumerable.Empty<string>());

            // 刷新音符UI
            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            guitar?.UpdateNoteDisplay();
        }

        protected override void OnUpgrade()
        {
            // 伤害次数在 OnPlay 中通过 IsUpgraded 自动处理
        }
    }
}