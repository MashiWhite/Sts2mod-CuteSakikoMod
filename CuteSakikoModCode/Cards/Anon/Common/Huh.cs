using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class Huh() : CuteAnonCard(2, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(5m, ValueProp.Move);
            yield return new RepeatVar(4);
            yield return new DynamicVar("notes", 1);
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Noteify.GetModCardKeyword()); }
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        var hitCount = DynamicVars["Repeat"].IntValue;
        var damage = DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(combat)
            .WithHitCount(hitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        ChordNoteSystem.Activate(Owner);

        var shuffleRng = Owner.RunState.Rng.Shuffle;
        var noteTypes = new[] { CardType.Attack, CardType.Skill, CardType.Power };
        var manualNoteCount = DynamicVars["notes"].IntValue;

        for (var i = 0; i < manualNoteCount; i++)
        {
            var randomType = noteTypes[shuffleRng.NextInt(noteTypes.Length)];
            await ChordNoteSystem.AddNoteAsync(Owner, randomType, choiceContext, triggerEffect: true);
        }

        ChordNoteUIManager.UpdateNoteDisplay(Owner);
        ChordNoteUIManager.UpdateStoredChordDisplay(Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["notes"].UpgradeValueBy(1);
    }
}