using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;


public class DifficultChoice() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PressurePower>(IsUpgraded ? 8m : 5m),
        new PowerVar<StrengthPower>(IsUpgraded ? 5m : 3m),
        new("Gold", IsUpgraded ? 45m : 35m)
    ];

    // 悬停提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 创建两个选项牌（规范实例，未加入任何牌堆）
        var pressureToken = ModelDb.Card<PressureOption>().ToMutable();
        var goldToken = ModelDb.Card<GoldOption>().ToMutable();

        // 根据主卡升级状态，升级对应的 token
        if (IsUpgraded)
        {
            pressureToken.UpgradeInternal();
            pressureToken.FinalizeUpgradeInternal();
            goldToken.UpgradeInternal();
            goldToken.FinalizeUpgradeInternal();
        }

        // 设置 token 的所有者为当前玩家（确保后续效果正确）
        pressureToken.Owner = Owner;
        goldToken.Owner = Owner;

        // 准备选择列表
        var options = new List<CardModel> { pressureToken, goldToken };

        // 弹出选择界面，让玩家选择一张
        var selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            options,
            Owner // 不允许跳过
        );

        if (selected == null) return;

        // 根据选择执行对应效果
        if (selected is PressureOption)
        {
            var pressureAmt = selected.DynamicVars["PressurePower"].IntValue;
            var strengthAmt = selected.DynamicVars["StrengthPower"].IntValue;
            await PowerCmd.Apply<PressurePower>(Owner.Creature, pressureAmt, Owner.Creature, this);
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, strengthAmt, Owner.Creature, this);
        }
        else if (selected is GoldOption)
        {
            var goldAmt = selected.DynamicVars["Gold"].IntValue;
            await PlayerCmd.GainGold(goldAmt, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}