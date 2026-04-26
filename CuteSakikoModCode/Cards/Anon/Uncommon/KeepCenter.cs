
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class KeepCenter() : CuteAnonCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(30m, ValueProp.Move);
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

            // 额外获得3个攻击音符
            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar != null)
            {
                var mainChords = guitar.GetCurrentChords();
                var bonusChords = guitar.GetBonusChords();
                var tempChords = guitar.GetTemporaryChords();
                var allBonus = bonusChords.Concat(tempChords);

                for (int i = 0; i < 3; i++)
                {
                    MusicNoteManager.AddNote(Owner, CardType.Attack, mainChords, allBonus);
                }

                // 刷新 UI
                guitar.UpdateNoteDisplay();
                guitar.UpdateStoredChordDisplay();
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(10m); // 30 → 40
        }
    }
}