using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public sealed class NameSense : CuteAnonCard
    {
        public NameSense() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
        {
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get { yield return CardKeyword.Ethereal; }
        }

        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get { yield return new CardsVar(1); }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();
            
            var targetPlayer = cardPlay.Target?.Player;
            if (targetPlayer == null)
                return;

            // 仅本地玩家（打出者）触发改名，但不等待，让卡牌立即结束
            if (LocalContext.IsMe(Owner))
            {
                _ = NameChangeCmd.ShowRenameDialog(targetPlayer);
            }
        }

        // 抽到时抽1张牌
        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            var cards = DynamicVars.Cards.BaseValue;
            await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
            if (card != this) return;
            await Cmd.Wait(0.25f);
            await CardPileCmd.Draw(choiceContext, cards, Owner);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(1);
        }
    }
}