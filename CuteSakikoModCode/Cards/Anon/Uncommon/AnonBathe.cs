
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class AnonBathe : CuteAnonCard
    {
        private static bool _isTransforming;

        public AnonBathe() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var debuffs = Owner.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
            if (debuffs.Any())
            {
                var toRemove = debuffs[Owner.RunState.Rng.CombatCardSelection.NextInt(debuffs.Count)];
                await PowerCmd.Remove(toRemove);
            }

            await Cmd.CustomScaledWait(0.1f, 0.15f);
            await TryCombine();
        }

        private async Task TryCombine()
        {
            // 使用类型作为锁对象，确保全局互斥
            var lockObj = typeof(AnonBathe);
            bool lockTaken = false;
            try
            {
                Monitor.Enter(lockObj, ref lockTaken);
                if (_isTransforming) return;
                _isTransforming = true;
            }
            finally
            {
                if (lockTaken) Monitor.Exit(lockObj);
            }

            try
            {
                if (CombatState == null || Owner == null) return;

                var hand = PileType.Hand.GetPile(Owner);
                if (hand == null) return;

                // 找到手牌中的 SakiBathe
                var sakiCard = hand.Cards.OfType<SakiBathe>().FirstOrDefault();
                if (sakiCard == null) return;

                bool shouldUpgrade = IsUpgraded || sakiCard.IsUpgraded;

                // 先移除对方卡牌
                if (sakiCard.Pile != null && sakiCard.Pile.IsCombatPile && sakiCard.Pile.Type != PileType.Play && sakiCard.Pile.Type != PileType.Exhaust)
                {
                    await CardPileCmd.RemoveFromCombat(sakiCard);
                }

                // 再移除自己（注意：自己可能已经被其他流程移走，需判空）
                if (this.Pile != null && this.Pile.IsCombatPile && this.Pile.Type != PileType.Play && this.Pile.Type != PileType.Exhaust)
                {
                    await CardPileCmd.RemoveFromCombat(this);
                }

                // 生成合成卡
                var combined = CombatState.CreateCard<AnonSakiBathe>(Owner);
                if (shouldUpgrade)
                {
                    combined.UpgradeInternal();
                    combined.FinalizeUpgradeInternal();
                }
                await CardPileCmd.AddGeneratedCardToCombat(combined, PileType.Hand, true);
            }
            finally
            {
                _isTransforming = false;
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}