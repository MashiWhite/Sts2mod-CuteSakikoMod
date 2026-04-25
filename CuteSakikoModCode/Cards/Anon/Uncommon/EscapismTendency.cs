
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class EscapismTendency : CuteAnonCard
    {
        public EscapismTendency() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new PowerVar<EscapismTendencyPower>(5m);
            }
        }

        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<EscapismTendencyPower>();
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            int amount = DynamicVars["EscapismTendencyPower"].IntValue;
            await PowerCmd.Apply<EscapismTendencyPower>(choiceContext,Owner.Creature, amount, Owner.Creature, this);

            var allies = CombatState.Players.Where(p => p != Owner && p.Creature.IsAlive).ToList();
            if (allies.Any())
            {
                var targetAlly = allies[CombatState.RunState.Rng.CombatCardSelection.NextInt(allies.Count)];
                // 使用目标玩家的 CombatState 创建卡牌实例，这是关键！
                var walkCard = targetAlly.Creature.CombatState.CreateCard<WalkHanding>(targetAlly);
                if (IsUpgraded) walkCard.UpgradeInternal();
                await CardPileCmd.AddGeneratedCardToCombat(walkCard, PileType.Hand, Owner);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars["EscapismTendencyPower"].UpgradeValueBy(3m); // 5-8
        }
    }
}