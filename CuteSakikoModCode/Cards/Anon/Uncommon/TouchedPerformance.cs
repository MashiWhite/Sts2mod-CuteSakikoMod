
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class TouchedPerformance() : CuteAnonCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                // 基础抽牌数 2，升级后变为 3
                yield return new CardsVar(2);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            // 抽牌
            int drawCount = DynamicVars["Cards"].IntValue;
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);

            // 演奏最新储存的和弦
            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar != null)
                await guitar.TriggerLastStoredChord(choiceContext);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["Cards"].UpgradeValueBy(1m); // 2 → 3
        }
    }
}