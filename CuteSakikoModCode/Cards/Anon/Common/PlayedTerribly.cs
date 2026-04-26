using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common
{
    public class PlayedTerribly() : CuteAnonCard(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(5m, ValueProp.Move);
            }
        }
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get { yield return HoverTipFactory.FromCard<LayFlat>(IsUpgraded); }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var combat = Owner.Creature.CombatState;
            if (combat == null) return;

            int hitCount = IsUpgraded ? 2 : 1;
            var damage = DynamicVars.Damage.BaseValue;
            var rng = Owner.RunState.Rng.CombatCardSelection;

            // 多次随机攻击
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

            // 随机清除一个音符
            MusicNoteManager.RemoveRandomNote(Owner, rng);

            // 添加躺平到手上
            var layFlatCard = CombatState.CreateCard<LayFlat>(Owner);
            if (IsUpgraded)
            {
                layFlatCard.UpgradeInternal();
                layFlatCard.FinalizeUpgradeInternal();
            }
            await CardPileCmd.AddGeneratedCardToCombat(layFlatCard, PileType.Hand, Owner);
        }

        protected override void OnUpgrade()
        {
            // 升级效果：攻击次数在 OnPlay 中通过 IsUpgraded 处理
        }
    }
}