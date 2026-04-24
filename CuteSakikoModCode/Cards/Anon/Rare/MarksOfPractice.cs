
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare
{
    public class MarksOfPractice() : CuteAnonCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            int stacks = IsUpgraded ? 2 : 1;
            await PowerCmd.Apply<MarksOfPracticePower>(Owner.Creature, stacks, Owner.Creature, this);
        }
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<MarksOfPracticePower>();
            }
        }

        protected override void OnUpgrade()
        {
            // 效果通过 IsUpgraded 自动处理
        }
    }
}