
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common
{
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

            // 额外获得一个攻击音符（需要传入真实的和弦列表才能触发匹配）
            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar != null)
            {
                var mainChords = guitar.GetCurrentChords();    // 已学习的主槽位和弦
                var bonusChords = guitar.GetBonusChords();     // Bonus 和弦
                var tempChords = guitar.GetTemporaryChords();  // 临时和弦（如碧天伴走）

                MusicNoteManager.AddNote(Owner, CardType.Attack,
                    mainChords,
                    bonusChords.Concat(tempChords));

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
}