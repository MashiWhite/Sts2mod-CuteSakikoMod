using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class Marionette() : CustomCardModel(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2) // 抽牌数和随机打出数（基础2，升级3）
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        
        
            // 1. 抽指定数量的牌
            var drawCount = DynamicVars.Cards.IntValue;
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);

            // 2. 获取当前手牌
            var handPile = PileType.Hand.GetPile(Owner);
            if (handPile == null) return;
            var handCards = handPile.Cards.ToList();
            if (handCards.Count == 0) return;

            // 3. 随机打出指定数量的手牌（不重复）
            var playCount = drawCount;
            var toPlay = new List<CardModel>();
            var indices = Enumerable.Range(0, handCards.Count).ToList();
            var rng = Owner.RunState.Rng.UpFront;
            for (var i = 0; i < playCount && indices.Count > 0; i++)
            {
                var idx = rng.NextInt(indices.Count);
                var cardIdx = indices[idx];
                toPlay.Add(handCards[cardIdx]);
                indices.RemoveAt(idx);
            }

            // 依次打出选中的牌
            foreach (var card in toPlay)
                await CardCmd.AutoPlay(choiceContext, card, null);

            // 4. 从弃牌堆中选择一张牌，置于抽牌堆顶
            var discardPile = PileType.Discard.GetPile(Owner);
            if (discardPile != null && discardPile.Cards.Count > 0)
            {
                var prefs = new CardSelectorPrefs(
                    new LocString("cards", "CUTESAKIKOMOD-MARIONETTE.selectionScreenPrompt"),
                    1,
                    1
                );
                // 强制手动确认，防止只有一张卡时自动跳过
                prefs = prefs with { RequireManualConfirmation = true };

                var selectedCards = await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    discardPile.Cards,
                    Owner,
                    prefs
                );
                var selected = selectedCards.FirstOrDefault();
                if (selected != null)
                {
                    // 从弃牌堆移除
                    selected.RemoveFromCurrentPile();
                    // 添加到抽牌堆顶部
                    await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
                }
            
        }

    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}