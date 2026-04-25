
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status
{
    [Pool(typeof(StatusCardPool))]
    public class NotNeeded() : CustomCardModel(1, CardType.Status, CardRarity.Status, TargetType.Self)
    {
        public override string PortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
            }
        }

        public override bool HasTurnEndInHandEffect => true;

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                // 基础格挡值 1，升级后变为 2
                yield return new BlockVar(1m, ValueProp.Move);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 打出时无效果
            await Task.CompletedTask;
        }

        public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
        {
            // 获取当前格挡值（升级后自动为 2）
            int blockAmount = DynamicVars.Block.IntValue;
            await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, null);

            // 复制自身并加入手牌
            var copy = CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
        }

        protected override void OnUpgrade()
        {
            // 升级：格挡值从 1 提升到 2
            DynamicVars.Block.UpgradeValueBy(1m);
        }
    }
}