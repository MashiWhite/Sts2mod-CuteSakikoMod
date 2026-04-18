using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

[Pool(typeof(TokenCardPool))]
public class BuyGold : CustomCardModel
{
    public BuyGold() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust, CardKeyword.Ethereal };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        var goldPrice = player.Creature.GetPower<HbkBankPower>()?.GoldPrice ?? 10;

        const int spendGold = 50;
        var currentGold = player.Gold;
        var goldToSpend = Math.Min(spendGold, currentGold);
        var goldShort = spendGold - goldToSpend;

        // 扣除金币
        if (goldToSpend > 0)
            await PlayerCmd.LoseGold(goldToSpend, player);

        // 获得黄金层数 = 50 / 金价（向下取整），与实际花费无关
        var goldGain = spendGold / goldPrice;
        if (goldGain > 0)
            await PowerCmd.Apply<GoldPower>(player.Creature, goldGain, player.Creature, this);

        // 负债 = 不足的金币数量（不除以金价）
        if (goldShort > 0)
            await PowerCmd.Apply<DebtPower>(player.Creature, goldShort, player.Creature, this);

        // 升级后抽一张牌
        if (IsUpgraded)
            await CardPileCmd.Draw(choiceContext, 1, player);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在逻辑中处理
    }
}