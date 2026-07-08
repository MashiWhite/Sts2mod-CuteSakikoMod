using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class NekoWant : CuteRanaCard
{
    public NekoWant() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(1); } // 生成的复制品数量
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 选择 1 张手牌（必须选，不能跳过）
        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_NEKO_WANT.selectionScreenPrompt"),
            1, 1);
        var chosen = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this);
        var original = chosen.FirstOrDefault();
        if (original == null) return;

        int copies = DynamicVars.Cards.IntValue;
        for (int i = 0; i < copies; i++)
        {
            var clone = original.CreateClone();      // 完美克隆：保留升级、附魔等所有状态
            clone.AddKeyword(CardKeyword.Exhaust);   // 额外赋予消耗
            await CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1); // 1 → 2
    }
}