
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class Wink : CuteAnonCard
{
    public Wink() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(10m, ValueProp.Move);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        // 先记录当前储存的和弦数量（演奏后会清空，所以提前记下）
        var storedCount = MusicNoteManager.GetStoredChords(Owner).Count;
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();

        // 演奏所有储存的和弦，并清空储存，但保留音符
        if (guitar != null)
            await guitar.TriggerAllStoredChordsKeepNotes(choiceContext);

        // 基础 1 次 + 每演奏一个和弦额外 1 次
        int totalHits = 1 + storedCount;
        var damage = DynamicVars.Damage.IntValue;

        await DamageCmd.Attack(damage)
            .FromCard(this,cardPlay)
            .WithHitCount(totalHits)
            .TargetingAllOpponents(combat)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 10 → 13
    }
}