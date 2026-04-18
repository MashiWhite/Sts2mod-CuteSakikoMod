using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Status;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class RedTea() : CustomCardModel(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    // 保留关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    // 动态变量：能量（基础1，升级2）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];

    // 悬停提示：显示“买单”卡牌
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 直接使用 FromCard<T> 的升级参数
            yield return HoverTipFactory.FromCard<MyTreat>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

        // 创建买单卡牌
        var myTreat = CombatState.CreateCard<MyTreat>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(myTreat);
        // 加入弃牌堆
        await CardPileCmd.AddGeneratedCardToCombat(myTreat, PileType.Discard, true);
    }

    protected override void OnUpgrade()
    {
        // 升级：能量+1（1→2）
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}