
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class WhatAboutMe() : CuteAnonCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(4m, ValueProp.Move);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var combat = Owner.Creature.CombatState;
            if (combat == null) return;

            // 获取本回合已获得的音符数（不包含本牌）
            int notesGained = MusicNoteManager.GetNotesGainedThisTurn(Owner);
            int additionalHits = notesGained; // 每1个音符多1次
            int totalHits = 1 + additionalHits;

            var damage = DynamicVars.Damage.BaseValue;
            var rng = Owner.RunState.Rng.CombatCardSelection;

            for (int i = 0; i < totalHits; i++)
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
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2m); // 4 → 6
        }
    }
}