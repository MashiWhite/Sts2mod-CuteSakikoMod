using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

[Pool(typeof(TokenCardPool))]
public class SellGold : CustomCardModel
{
    public SellGold() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust, CardKeyword.Ethereal };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        var goldPrice = player.Creature.GetPower<HbkBankPower>()?.GoldPrice ?? 10;

        var goldPower = player.Creature.GetPower<GoldPower>();
        var currentGoldLayers = goldPower?.Amount ?? 0;
        if (currentGoldLayers <= 0) return;

        var goldToSell = currentGoldLayers; // 全部卖出

        // 减少黄金层数
        await PowerCmd.ModifyAmount(goldPower, -goldToSell, player.Creature, this);
        // 获得金币 = 卖出层数 × 金价
        var goldGain = goldToSell * goldPrice;
        await PlayerCmd.GainGold(goldGain, player);

        // 升级后获得1点能量
        if (IsUpgraded)
            await PlayerCmd.GainEnergy(1, player);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在逻辑中处理
    }
}