using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

public class SellGold() : OtherModTokenCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
#if STS2_AT_LEAST_0_113_0
    // 0.113 Blitz（闪局）禁用：闪局没有金币经济（商店改成免费 6 选 3），金币/黄金交易类卡牌无意义
    public override bool IsAllowedInBlitz => false;
#endif
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Ethereal];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        var goldPrice = player.Creature.GetPower<HbkBankPower>()?.GoldPrice ?? 10;

        var goldPower = player.Creature.GetPower<GoldPower>();
        var currentGoldLayers = goldPower?.Amount ?? 0;
        if (currentGoldLayers <= 0) return;

        var goldToSell = currentGoldLayers; // 全部卖出

        // 减少黄金层数
        await PowerCmd.ModifyAmount(choiceContext, goldPower, -goldToSell, player.Creature, this);
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