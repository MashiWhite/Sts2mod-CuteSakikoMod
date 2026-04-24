
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common
{
    public class Resentful() : CuteAnonCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(8m, ValueProp.Move);
            }
        }
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get { yield return HoverTipFactory.FromCard<NotNeeded>(IsUpgraded); }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;

            TriggerBanter();

            // 造成伤害
            var damage = DynamicVars.Damage.BaseValue;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 生成“不需要我了吧”并加入手牌（使用 CombatState.CreateCard）
            var newCard = CombatState.CreateCard<NotNeeded>(Owner);
            if (IsUpgraded)
            {
                newCard.UpgradeInternal();
                newCard.FinalizeUpgradeInternal();
            }

            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, true);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m); // 8 → 11
        }
    }
}
        
 

      