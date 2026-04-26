
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class StageJitters() : CuteAnonCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                // 基础伤害：未升级15，升级后18
                yield return new CalculationBaseVar(15m);
                // 每个音符/和弦降低的伤害值
                yield return new ExtraDamageVar(2m);
                // 最终计算后的伤害（实时显示）
                yield return new CalculatedDamageVar(ValueProp.Move)
                    .WithMultiplier((card, _) =>
                    {
                        if (card.Owner == null) return 0m;
                        int noteCount = MusicNoteManager.GetCurrentNotes(card.Owner).Count;
                        int chordCount = MusicNoteManager.GetStoredChords(card.Owner).Count;
                        // 每储存一个音符或和弦就降低2点伤害，倍率为负数
                        return -(decimal)(noteCount + chordCount);
                    });
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            TriggerBanter();

            // 使用计算后的伤害值进行攻击
            var damage = DynamicVars.CalculatedDamage;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            // 基础伤害提升 15 → 18
            DynamicVars.CalculationBase.UpgradeValueBy(3m);
        }
    }
}