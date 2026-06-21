using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class DontRun() : CuteAnonCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(10m, ValueProp.Move); }
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

        // 额外获得攻击音符
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
        {
            var mainChords = guitar.GetCurrentChords();
            var bonusChords = guitar.GetBonusChords();
            var tempChords = guitar.GetTemporaryChords();
            
            int manualNoteCount = IsUpgraded ? 3 : 2;
            for (int i = 0; i < manualNoteCount; i++)
            {
                await MusicNoteManager.AddNoteAndAutoPlayAsync(Owner, CardType.Attack,
                    mainChords,
                    bonusChords.Concat(tempChords),choiceContext);
            }

            // 刷新 UI
            guitar.UpdateNoteDisplay();
            guitar.UpdateStoredChordDisplay();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 10 → 13
    }
}