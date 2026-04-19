
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare
{
    public class LittleMoments : CuteAnonCard
    {
        private bool _isTransforming;

        public LittleMoments() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromCard<Lifetime>(IsUpgraded);
            }
        }
        
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new CardsVar(2);
            }
        }

          protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            // 抽牌
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

            // 创建复制加入抽牌堆
            if (CombatState != null && Owner != null)
            {
                var copy = CombatState.CreateCard<LittleMoments>(Owner);
                if (IsUpgraded)
                {
                    copy.UpgradeInternal();
                    copy.FinalizeUpgradeInternal();
                }
                await CardPileCmd.Add(copy, PileType.Draw, CardPilePosition.Random);
            }

            // 等待操作完成
            await Cmd.CustomScaledWait(0.1f, 0.15f);

            // 检查转化
            await TryTransform();
        }

        private async Task TryTransform()
        {
            if (_isTransforming) return;
            if (CombatState == null || Owner == null) return;

            // 获取玩家所有战斗中的卡牌
            var allCards = Owner.PlayerCombatState?.AllCards;
            if (allCards == null) return;

            var littleMomentsCards = allCards.OfType<LittleMoments>().ToList();
            if (littleMomentsCards.Count < 5) return;

            _isTransforming = true;

            // 移除所有小小的瞬间（包括自身）
            foreach (var card in littleMomentsCards)
            {
                if (card.Pile != null)
                {
                    await CardPileCmd.RemoveFromCombat(card);
                }
            }

            // 生成一辈子
            var lifetime = CombatState.CreateCard<Lifetime>(Owner);
            if (IsUpgraded)
            {
                lifetime.UpgradeInternal();
                lifetime.FinalizeUpgradeInternal();
            }
            await CardPileCmd.AddGeneratedCardToCombat(lifetime, PileType.Hand, true);

            _isTransforming = false;
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}