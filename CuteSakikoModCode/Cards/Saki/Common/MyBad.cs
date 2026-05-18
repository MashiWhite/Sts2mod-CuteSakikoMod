using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

// 需要这个来创建 LocString

// 【新增】需要这个命名空间来使用 CardSelectorPrefs

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class MyBad() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 获取弃牌堆
        var discardPile = PileType.Discard.GetPile(Owner);
        if (discardPile == null || discardPile.Cards.Count == 0)
            return;

        // 自定义选择提示（使用本地化键）
        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_MY_BAD.selectionScreenPrompt"),
            1,
            1
        );
        prefs = prefs with { RequireManualConfirmation = true };

        var selectedCards = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            discardPile.Cards,
            Owner,
            prefs
        );

        var selected = selectedCards.FirstOrDefault();
        if (selected != null) await CardCmd.Exhaust(choiceContext, selected);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}