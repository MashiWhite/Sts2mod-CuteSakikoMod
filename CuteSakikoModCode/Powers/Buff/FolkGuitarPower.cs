using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{
    public class FolkGuitarPower : CuteSakikoModPower, IChordSequenceModifierProvider
    {
        private bool _upgraded;

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        public void SetUpgraded(bool upgraded) => _upgraded = upgraded;

        // ===== 每回合开始时给一张打板 =====
        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player.Creature != Owner)
                return;

            var card = CombatState.CreateCard<PercussiveFingerstyle>(player);
            if (_upgraded)
            {
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        }

        // ===== 和弦修改器：所有已学习和弦的第一个音符变为 *（特殊音符） =====
        public IEnumerable<ChordSequenceModifier> GetModifiers(Creature owner)
        {
            yield return new ReplaceNoteModifier(0, CardType.Status);
        }

        public IEnumerable<ChordCategory>? AffectedCategories => null; // 影响所有类别
    }
}