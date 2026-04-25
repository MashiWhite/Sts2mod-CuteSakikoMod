using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Uncommon;

public class HbkBank : CuteSakikoModEggCard
{
    public HbkBank() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }
    

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 根据升级状态显示对应的卡牌悬停提示
            yield return HoverTipFactory.FromCard<BuyGold>(IsUpgraded);
            yield return HoverTipFactory.FromCard<SellGold>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = Owner.Creature.GetPower<HbkBankPower>();
        if (power == null) await PowerCmd.Apply<HbkBankPower>(choiceContext,Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在生成的 Token 卡和 Power 的金价波动范围中体现
    }
}