using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Rare;

public class HbkBank() : CuteSakikoModEggCard(0, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 根据升级状态显示对应的卡牌悬停提示
            yield return HoverTipFactory.FromCard<BuyGold>(IsUpgraded);
            yield return HoverTipFactory.FromCard<SellGold>(IsUpgraded);
            yield return HoverTipFactory.FromCard<GoldBrick>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = Owner.Creature.GetPower<HbkBankPower>();
        if (power == null) await PowerCmd.Apply<HbkBankPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在生成的 Token 卡和 Power 的金价波动范围中体现
    }
}