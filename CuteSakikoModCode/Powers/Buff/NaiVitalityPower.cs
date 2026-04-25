
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{
    public class NaiVitalityPower : CuteSakikoModPower
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        private bool _willTriggerNextTurn;

        // 回合结束时判断
        public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
        {
            if (side != CombatSide.Player) return;

            var combatState = Owner?.Player?.PlayerCombatState;
            if (combatState == null) return;

            // OR 逻辑：能量 > 0 或手牌数 > 0
            bool hasEnergy = combatState.Energy > 0;
            int handCount = combatState.Hand?.Cards.Count ?? 0;
            _willTriggerNextTurn = hasEnergy || handCount > 0;

            await Task.CompletedTask;
        }

        // 下回合开始时触发（正确的重载签名）
        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (!_willTriggerNextTurn) return;
            _willTriggerNextTurn = false;

            if (player == null) return;

            await PlayerCmd.GainEnergy(1, player);
            await CardPileCmd.Draw(choiceContext, 1, player);
        }
    }
}