using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Uncommon;


public class CccHibiki() : CuteSakikoModEggCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(13m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 根据升级状态显示对应的真·春日影卡牌悬停提示
            yield return HoverTipFactory.FromCard<TrueHaruhikage>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 创建真·春日影卡牌
        var trueHaru = CombatState.CreateCard<TrueHaruhikage>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(trueHaru);
        // 加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(trueHaru, PileType.Hand, true);
    }

    protected override void OnUpgrade()
    {
        // 升级：格挡 13 → 18
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}