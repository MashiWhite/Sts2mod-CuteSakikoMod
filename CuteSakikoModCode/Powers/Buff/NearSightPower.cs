
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{
    public class NearSightPower : CuteSakikoModPower
    {
        private bool _upgraded;

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        public void SetUpgraded(bool upgraded) => _upgraded = upgraded;

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player.Creature != Owner)
                return;

            // 使用 player 参数创建卡牌
            var card = CombatState.CreateCard<CloseObserve>(player);
            if (_upgraded)
            {
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }
            // 正确的参数顺序：(card, pileType, addedByPlayer)
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner.Player);
        }
    }
}