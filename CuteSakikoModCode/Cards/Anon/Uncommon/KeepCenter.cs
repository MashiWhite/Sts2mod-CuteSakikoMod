using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class KeepCenter() : CuteAnonCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(30m, ValueProp.Move);
            yield return new DynamicVar("Notes", 4m); // 初始获得4个音符
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        TriggerBanter();

        // 造成伤害
        var damage = DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 额外获得攻击音符（次数由动态变量 Notes 决定）
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
        {
            var mainChords = guitar.GetCurrentChords();
            var bonusChords = guitar.GetBonusChords();
            var tempChords = guitar.GetTemporaryChords();
            var allBonus = bonusChords.Concat(tempChords);

            var noteCount = (int)DynamicVars["Notes"].BaseValue;
            for (var i = 0; i < noteCount; i++)
                MusicNoteManager.AddNote(Owner, CardType.Attack, mainChords, allBonus);

            // 刷新 UI
            guitar.UpdateNoteDisplay();
            guitar.UpdateStoredChordDisplay();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m); // 伤害 30 → 40
        DynamicVars["Notes"].UpgradeValueBy(1m); // 音符 4 → 5
    }
}