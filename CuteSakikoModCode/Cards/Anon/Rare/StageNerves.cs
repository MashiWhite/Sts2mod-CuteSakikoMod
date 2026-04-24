using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare
{
    public class StageNerves() : CuteAnonCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<StageNervesPower>();
            }
        }

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new PowerVar<StageNervesPower>(1m);
                yield return new DynamicVar("Block", 1m);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            int amount = DynamicVars["StageNervesPower"].IntValue;
            await PowerCmd.Apply<StageNervesPower>(Owner.Creature, amount, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["StageNervesPower"].UpgradeValueBy(1m); // 1 → 2
            DynamicVars["Block"].UpgradeValueBy(1m);           // 1 → 2
            AddKeyword(CardKeyword.Innate);
        }
    }
}