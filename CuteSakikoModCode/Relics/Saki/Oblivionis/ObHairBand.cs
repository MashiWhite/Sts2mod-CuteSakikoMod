using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Oblivionis;

public sealed class ObHairBand : ObMask
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 进化后每次遗忘造成6点伤害
    protected override int DamagePerForgottenCard => 6;

    protected override async Task OnCardsForgotten(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> cards,
        CardModel? source)
    {
        if (Owner == null || cards.Count == 0) return;
        if (cards[0].Owner != Owner) return;

        // 伤害：每遗忘一张牌对所有敌人造成一次伤害（继承基类实现）
        await ApplyDamageForForgottenCards(choiceContext, cards);

        // 抽牌：每遗忘一张牌抽一张牌
        await CardPileCmd.Draw(choiceContext, cards.Count, Owner);
    }
}