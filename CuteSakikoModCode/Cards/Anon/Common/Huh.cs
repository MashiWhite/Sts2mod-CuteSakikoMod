using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
        await DamageCmd.Attack(damage)
            .FromCard(this,cardPlay)
            .TargetingRandomOpponents(combat)
            .WithHitCount(_hitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
        {
            var shuffleRng = Owner.RunState.Rng.Shuffle;
            var noteTypes = new[] { CardType.Attack, CardType.Skill, CardType.Power };
            var randomType = noteTypes[shuffleRng.NextInt(noteTypes.Length)];
            var allChords = guitar.GetAllEquippedChords();

            int manualNoteCount = IsUpgraded ? 3 : 2;
            for (int i = 0; i < manualNoteCount; i++)
                await MusicNoteManager.AddNoteAndAutoPlayAsync(Owner, randomType, allChords, choiceContext);

            guitar.UpdateNoteDisplay();
            guitar.UpdateStoredChordDisplay();
        }
    }

    protected override void OnUpgrade()
    {
        _hitCount = 5;
    }
}